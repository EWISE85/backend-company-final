using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ISmallCollectionPointsService
    {
		Task<bool> AddNewSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints);
		Task<bool> UpdateSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints);

		Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId);

		Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointByCompanyId(string companyId);

		Task<SmallCollectionPointsResponse> GetSmallCollectionById(string smallCollectionPointId);

		Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword, string email);

		Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model);

		Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointActive();

	}
}
