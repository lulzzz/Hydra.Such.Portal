﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Hydra.Such.Data.ViewModel.Compras;
using Hydra.Such.Data.Logic.Compras;
using Microsoft.Extensions.Options;
using Hydra.Such.Portal.Configurations;
using Hydra.Such.Data.NAV;

namespace Hydra.Such.Portal.Areas.Compras.Controllers
{
    public class PreRequisicoesController : Controller
    {
        [Area("Compras")]
        public IActionResult Index()
        {
            return View();
        }

        public JsonResult CBVehicleFleetBool([FromBody] int id)
        {
            bool? FleetBool = DBRequesitionType.GetById(id).Frota;
            return Json(FleetBool);
        }

        public JsonResult GetPlaceData([FromBody] int placeId)
        {
            bool? FleetBool = DBRequesitionType.GetById(placeId).Frota;
            return Json(FleetBool);
        }

    }
}