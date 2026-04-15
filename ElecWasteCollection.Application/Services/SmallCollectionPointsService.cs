using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class SmallCollectionPointsService : ISmallCollectionPointsService
    {
		private readonly ISmallCollectionPointsRepository _smallCollectionRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IAccountRepsitory _accountRepository;
		public SmallCollectionPointsService(IUnitOfWork unitOfWork, IUserRepository userRepository, IAccountRepsitory accountRepository, ISmallCollectionPointsRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_accountRepository = accountRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}

		public async Task<bool> Active(string id)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == id);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			smallPoint.Status = SmallCollectionPointStatus.DANG_HOAT_DONG.ToString();
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> AddNewSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			await _unitOfWork.SmallCollectionPoints.AddAsync(smallCollectionPoints);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword, string email)
		{
			var result = new ImportResult();

			var existingCompany = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPoints.SmallCollectionPointsId);
			if (existingCompany != null)
			{
				await UpdateSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Đã cập nhật thông tin kho '{smallCollectionPoints.Name}'.");
				result.IsNew = false;
			}
			else
			{
				await AddNewSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Thêm kho '{smallCollectionPoints.Name}' thành công.");

                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, "10");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.SmallCollectionPointsId, SystemConfigKey.WAREHOUSE_LOAD_THRESHOLD, "0.7");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.SmallCollectionPointsId, SystemConfigKey.TRANSPORT_SPEED, "35");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, "10");

                var newAdminWarehouse = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = null,
					Email = email,
					Name = "Admin " + smallCollectionPoints.Name,
					Role = UserRole.AdminWarehouse.ToString(),
					Status = UserStatus.DANG_HOAT_DONG.ToString(),
					CollectionCompanyId = smallCollectionPoints.CompanyId,
					CreateAt = DateTime.UtcNow,
					SmallCollectionPointsId = smallCollectionPoints.SmallCollectionPointsId,
				};
				await _unitOfWork.Users.AddAsync(newAdminWarehouse);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminWarehouse.UserId,
					Username = adminUsername,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
					IsFirstLogin = true
				};
				await _unitOfWork.Accounts.AddAsync(adminAccount);
				result.Messages.Add($"Tạo tài khoản quản trị kho với tên đăng nhập '{adminUsername}'.");
				result.IsNew = true;
				await _unitOfWork.SaveAsync();
			}

			return result;
		}

		private async Task UpsertPointConfigAsync(string companyId, string pointId, SystemConfigKey key, string value)
		{
			var existingConfig = await _unitOfWork.SystemConfig.GetAsync(x =>
				x.Key == key.ToString() &&
				x.SmallCollectionPointsId == pointId);

			if (existingConfig != null)
			{
				existingConfig.Value = value;
				_unitOfWork.SystemConfig.Update(existingConfig);
			}
			else
			{
				var newConfig = new SystemConfig
				{
					SystemConfigId = Guid.NewGuid(),
					Key = key.ToString(),
					Value = value,
					CompanyId = companyId,
                    SmallCollectionPointsId = pointId,
					Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
					DisplayName = key.ToString(),
					GroupName = "PointConfig"
                };
				await _unitOfWork.SystemConfig.AddAsync(newConfig);
			}
		}


        public async Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho",404);
			smallPoint.Status = SmallCollectionPointStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model)
		{
			string statusEnum = null;
			if (!string.IsNullOrEmpty(model.Status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<SmallCollectionPointStatus>(model.Status).ToString();
			}
			var (entities, totalItems) = await _smallCollectionRepository.GetPagedAsync(
				companyId: model.CompanyId,
				status: statusEnum,
				page: model.Page,
				limit: model.Limit
			);
			var resultList = entities.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.SmallCollectionPointsId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return new PagedResultModel<SmallCollectionPointsResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}

		public async Task<SmallCollectionPointsResponse> GetSmallCollectionById(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			
				return new SmallCollectionPointsResponse
				{
					Id = smallPoint.SmallCollectionPointsId,
					CompanyId = smallPoint.CompanyId,
					Name = smallPoint.Name,
					Address = smallPoint.Address,
					Latitude = smallPoint.Latitude,
					Longitude = smallPoint.Longitude,
					OpenTime = smallPoint.OpenTime,
					Status = smallPoint.Status
				};

		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointActive()
		{
			var smallPoints = await _smallCollectionRepository.GetAllAsync(
				filter: s => s.Status == SmallCollectionPointStatus.DANG_HOAT_DONG.ToString(),
				includeProperties: "RecyclingCompany.CompanyRecyclingCategories.Category.SubCategories"
			);

			return smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.SmallCollectionPointsId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status,
				CompanyName = point.CollectionCompany?.Name,

				AcceptedCategories = point.RecyclingCompany?.CompanyRecyclingCategories
					.Where(crc => crc.Category.Status == CategoryStatus.HOAT_DONG.ToString())
					.SelectMany(crc => crc.Category.SubCategories) 
					.Where(sub => sub.Status == CategoryStatus.HOAT_DONG.ToString()) 
					.Select(sub => new CategoryModel
					{
						Id = sub.CategoryId,
						Name = sub.Name
					}).ToList() ?? new List<CategoryModel>()
			}).ToList();
		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointByCompanyId(string companyId)
		{
			var smallPoints = await _smallCollectionRepository.GetsAsync(s => s.CompanyId == companyId);
			var result = smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.SmallCollectionPointsId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return result;
		}

		public async Task<bool> UnActive(string id)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == id);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			smallPoint.Status = SmallCollectionPointStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> UpdateSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPoints.SmallCollectionPointsId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			var statusEnum = StatusEnumHelper.GetValueFromDescription<SmallCollectionPointStatus>(smallCollectionPoints.Status).ToString();
			smallPoint.Name = smallCollectionPoints.Name;
			smallPoint.Address = smallCollectionPoints.Address;
			smallPoint.Latitude = smallCollectionPoints.Latitude;
			smallPoint.Longitude = smallCollectionPoints.Longitude;
			smallPoint.Status = statusEnum.ToString();
			smallPoint.CompanyId = smallCollectionPoints.CompanyId;
			smallPoint.OpenTime = smallCollectionPoints.OpenTime;
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
