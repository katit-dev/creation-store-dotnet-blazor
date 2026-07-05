using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreationStore.API.Data;
using Microsoft.AspNetCore.Mvc;
//using CreationStore.API.Models;

namespace CreationStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly CreationStoreDbContext _context;

        public ProductsController(CreationStoreDbContext context)
        {
            _context = context;
        }
        
        

    }
}