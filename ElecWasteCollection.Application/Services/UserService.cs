using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.UserModel;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class UserService : IUserService
	{
		private readonly IFirebaseService _firebaseService;
		private readonly ITokenService _tokenService;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductRepository _productRepository;
		private readonly IUserVoucherRepository _userVoucherRepository;


		public UserService(IFirebaseService firebaseService, ITokenService tokenService, IUserRepository userRepository, IUnitOfWork unitOfWork, IProductRepository productRepository, IUserVoucherRepository userVoucherRepository)
		{
			_firebaseService = firebaseService;
			_tokenService = tokenService;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
			_productRepository = productRepository;
			_userVoucherRepository = userVoucherRepository;
		}

		public async Task<List<UserResponse>> GetAll()
		{
			var users = await _userRepository.GetsAsync(u => u.Role == UserRole.User.ToString());
			if (users == null || users.Count == 0)
			{
				return new List<UserResponse>();
			}
			var userResponses = users.Select(u => new UserResponse
			{
				UserId = u.UserId,
				Name = u.Name,
				Email = u.Email,
				Phone = u.Phone,
				Avatar = u.Avatar,
				Role = u.Role,
				SmallCollectionPointId = u.SmallCollectionPointsId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(u.Status).ToString()

			}).ToList();
			return userResponses;
		}

		public void AddRange(IEnumerable<User> newUsers)
		{
			throw new NotImplementedException();
		}

		public async void AddUser(User user)
		{
			var existingEmail = _userRepository.GetAsync(u => u.Email == user.Email);
			if (existingEmail != null) throw new AppException("Email đã được liên kết với tài khoản khác, vui lòng thử email khác", 400);
			var repo = _unitOfWork.Users;
			await repo.AddAsync(user);
			await _unitOfWork.SaveAsync();
		}

		public async Task<UserResponse> GetById(Guid id)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == id);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var userResponse = new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				CreateAt = user.CreateAt,
				Points = user.Points,
				SmallCollectionPointId = user.SmallCollectionPointsId,
				CollectionCompanyId = user.CollectionCompanyId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(user.Status).ToString()
			};
			return userResponse;
		}

		//public void UpdateUser(int iat, int ing, Guid id)
		//{
		//	var user = users.FirstOrDefault(u => u.UserId == id);
		//	if (user != null)
		//	{
		//		user.Iat = iat;
		//		user.Ing = ing;
		//	}
		//}

		

		public async Task<UserProfileResponse> Profile(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var smallCollectionPointName = await _unitOfWork.SmallCollectionPoints.GetAsync(s => s.SmallCollectionPointsId == user.SmallCollectionPointsId);
			var collectionCompanyName = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == user.CollectionCompanyId);
			//UserSettingsModel settingsObj;
			//if (string.IsNullOrEmpty(user.Preferences))
			//{
			//	settingsObj = new UserSettingsModel { ShowMap = false };
			//}
			//else
			//{
			//	try
			//	{
			//		settingsObj = JsonSerializer.Deserialize<UserSettingsModel>(user.Preferences)?? new UserSettingsModel { ShowMap = false };
			//	}
			//	catch
			//	{
			//		settingsObj = new UserSettingsModel { ShowMap = false };
			//	}
			//}
			var userProfile = new UserProfileResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				Points = user.Points,
				CollectionCompanyId = user.CollectionCompanyId,
				SmallCollectionPointId = user.SmallCollectionPointsId,
				SmallCollectionName = smallCollectionPointName?.Name,
				CompanyName = collectionCompanyName?.Name,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(user.Status).ToString()
			};
			return userProfile;
		}

		public async Task<UserResponse?> GetByPhone(string phone)
		{
			var user = await _userRepository.GetAsync(u => u.Phone == phone);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var userResponse = new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				SmallCollectionPointId = user.SmallCollectionPointsId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(user.Status).ToString()
			};
			return userResponse;
		}

		public async Task<bool> UpdateProfile(UserProfileUpdateModel model)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == model.UserId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			user.Email = model.Email ?? user.Email;
			user.Avatar = model.AvatarUrl ?? user.Avatar;
			user.Phone = model.phoneNumber ?? user.Phone;
			//user.Preferences = JsonSerializer.Serialize(model.Settings);
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> DeleteUser(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			user.Status = UserStatus.KHONG_HOAT_DONG.ToString();
			user.AppleId = null;
			user.Email = null;
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<UserResponse?> GetByEmailOrPhone(string infomation)
		{
			Guid parsedId;
			bool isGuid = Guid.TryParse(infomation, out parsedId);

			var user = await _userRepository.GetAsync(u =>
				u.Email == infomation ||
				u.Phone == infomation ||
				(isGuid && u.UserId == parsedId)
			);

			if (user == null) throw new AppException("User không tồn tại", 404);

			var userResponse = new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Points = user.Points,
				Avatar = user.Avatar,
				Role = user.Role,
				SmallCollectionPointId = user.SmallCollectionPointsId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(user.Status).ToString()
			};

			return userResponse;
		}

		public async Task<List<UserResponse>> GetByEmail(string email)
		{
			var users = await _userRepository.GetsAsync(u => u.Email != null && u.Email.Contains(email));

			if (users == null || users.Count == 0)
			{
				throw new AppException("User không tồn tại", 404);
			}

			var userResponses = users.Select(u => new UserResponse
			{
				UserId = u.UserId,
				Name = u.Name,
				Email = u.Email,
				Phone = u.Phone,
				Avatar = u.Avatar,
				Role = u.Role,
				SmallCollectionPointId = u.SmallCollectionPointsId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(u.Status).ToString()
			}).ToList();

			return userResponses;
		}

		public async Task<bool> BanUser(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			user.Status = UserStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<PagedResultModel<UserResponse>> AdminFilterUser(AdminFilterUserModel model)
		{
			string? statusEnum = null;
			if (model.Status != null)
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<UserStatus>(model.Status).ToString();
			}

			// Nhận về Tuple (users, totalCount)
			var result = await _userRepository.AdminFilterUser(
				model.Page,
				model.Limit,
				model.FromDate,
				model.ToDate,
				model.Email,
				statusEnum
			);

			// result.Users là danh sách user
			// result.TotalCount là tổng số bản ghi

			var userResponses = result.Users.Select(u => new UserResponse
			{
				UserId = u.UserId,
				Name = u.Name,
				Email = u.Email,
				Phone = u.Phone,
				Avatar = u.Avatar,
				Role = u.Role,
				SmallCollectionPointId = u.SmallCollectionPointsId,
				CreateAt = u.CreateAt,
				Points = u.Points,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(u.Status).ToString()
			}).ToList();

			// Truyền result.TotalCount vào PagedResultModel
			return new PagedResultModel<UserResponse>(userResponses, model.Page, model.Limit, result.TotalCount);
		}

		public async Task<bool> UpdatePointForUser(Guid userId, double pointToAdd)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);

			if (user == null)
			{
				throw new AppException("Không tìm thấy người dùng", 404);
			}
			if (user.Points + pointToAdd < 0)
			{
				throw new AppException("User không đủ điểm để thực hiện điều chỉnh này", 400);
			}

			user.Points += pointToAdd;

			_unitOfWork.Users.Update(user);

			return true;
		}
		public async Task<UserPointModel> GetPointByUserId(Guid userId)
		{
			var userPoint = await _userRepository.GetAsync(up => up.UserId == userId);
			if (userPoint == null) throw new AppException("Không tìm thấy điểm người dùng", 404);
			var userPointModel = new UserPointModel
			{
				UserId = userPoint.UserId,
				Points = userPoint.Points
			};
			return userPointModel;
		}

		public async Task<PagedResultModel<UserResponse>> FilterUserByRadius(string smallCollectionPointId, int page = 1, int limit = 10)
		{
			var warehouse = await _unitOfWork.SmallCollectionPoints
							.GetAsync(w => w.SmallCollectionPointsId == smallCollectionPointId);
			if (warehouse == null)
			{
				throw new AppException("Không tìm thấy điểm thu gom nhỏ", 404);
			}
			var radiusConfig = await _unitOfWork.SystemConfig
				.GetAsync(c => c.Key == SystemConfigKey.RADIUS_FOR_USER_FILTER.ToString()
									   && c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());
			if (radiusConfig == null)
			{
				throw new AppException("Không tìm thấy cấu hình bán kính", 404);
			}
			double radiusKm = 5.0;
			if (radiusConfig != null && double.TryParse(radiusConfig.Value, out double parsedRadius))
			{
				radiusKm = parsedRadius;
			}
			var (users, totalItems) = await _userRepository.GetUsersByRadiusAsync(
				warehouse.Latitude,
				warehouse.Longitude,
				radiusKm,
				page,
				limit);
			var userResponses = users.Select(user => new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Points = user.Points,
				Role = user.Role,
				SmallCollectionPointId = user.SmallCollectionPointsId,
				CollectionCompanyId = user.CollectionCompanyId,
				CreateAt = user.CreateAt,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<UserStatus>(user.Status).ToString()
			}).ToList();

			return new PagedResultModel<UserResponse>(userResponses, page, limit, totalItems);
		}

		public async Task<bool> UpdatePointForUserByAdminSystem(Guid userId, double pointToAdd)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null)
			{
				throw new AppException("Không tìm thấy người dùng", 404);
			}
			user.Points = pointToAdd;
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<PagedResultModel<UserProductModel>> GetProductsByUserIdPaginatedAsync(Guid userId, int page, int limit)
		{
			var (items, totalCount) = await _productRepository.GetUserProductsPaginatedAsync(userId, page, limit);

			var mappedItems = items.Select(p => new UserProductModel
			{
				ProductId = p.ProductId,
				Description = p.Description,
				CreateAt = p.CreateAt,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(p.Status).ToString(),
				CategoryName = p.Category?.Name,
				BrandName = p.Brand?.Name,
				FinalPoints = p.PointTransactions != null ? p.PointTransactions.Sum(pt => pt.Point) : 0,
				SmallCollectionPointName = p.SmallCollectionPoints?.Name
			}).ToList();

			return new PagedResultModel<UserProductModel>(mappedItems, page, limit, totalCount);
		}
		public async Task<PagedResultModel<UserVoucherModel>> GetVouchersByUserIdPaginatedAsync(Guid userId, int page, int limit)
		{
			var (items, totalCount) = await _userVoucherRepository.GetUserVouchersPaginatedAsync(userId, page, limit);

			// Map dữ liệu sang Model
			var mappedItems = items.Select(item => new UserVoucherModel
			{
				UserVoucherId = item.UV.UserVoucherId,
				VoucherId = item.UV.VoucherId,
				Code = item.UV.Voucher?.Code,
				Name = item.UV.Voucher?.Name,
				ImageUrl = item.UV.Voucher?.ImageUrl,
				ReceivedAt = item.UV.ReceivedAt,

				PointsToRedeem = item.PointsUsed
			}).ToList();

			return new PagedResultModel<UserVoucherModel>(mappedItems, page, limit, totalCount);
		}
	}
}
