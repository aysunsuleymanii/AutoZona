using AutoZona.Domain.DomainModels;
using AutoZona.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoZona.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ICarListingService _carListingService;

        public AdminController(ICarListingService carListingService)
        {
            _carListingService = carListingService;
        }


        // GET Action for getting all car listings which are available
        [HttpGet("[action]")]
        public Task<IEnumerable<CarListing>> GetAllActiveCarListings()
        {
            return _carListingService.GetAllActiveCarListingsAsync();
        }


        // Details for a single car listing
        [HttpGet("[action]/{id}")]
        public Task<CarListing?> GetCarListingById(Guid id)
        {
            return _carListingService.GetCarListingByIdAsync(id);
        }
    }
}