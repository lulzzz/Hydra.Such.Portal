﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Hydra.Such.Portal.Controllers
{
    public class NutricaoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Projetos()
        {
            return View();
        }

        public IActionResult Contratos()
        {
            return View();
        }

        public IActionResult Requisicoes()
        {
            return View();
        }

        public IActionResult FichasTecnicasPratos()
        {
            return View();
        }

        public IActionResult TabelasAuxiliares()
        {
            return View();
        }
    }
}