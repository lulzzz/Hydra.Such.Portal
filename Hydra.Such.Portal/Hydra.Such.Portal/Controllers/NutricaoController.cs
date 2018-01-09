﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hydra.Such.Data.ViewModel;
using Hydra.Such.Data.Logic;
using Newtonsoft.Json.Linq;
using Hydra.Such.Data.Database;
using Hydra.Such.Data.Logic.Nutrition;
using Hydra.Such.Data.ViewModel.Nutrition;
using Hydra.Such.Data.Logic.Contracts;
using Hydra.Such.Portal.Configurations;
using Microsoft.Extensions.Options;

namespace Hydra.Such.Portal.Controllers
{
    [Authorize]
    public class NutricaoController : Controller
    {
        private readonly NAVConfigurations _config;
        public NutricaoController(IOptions<NAVConfigurations> appSettings)
        {
            _config = appSettings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region Projetos
        public IActionResult Projetos()
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 1);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult DetalhesProjeto(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 1);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProjectNo = id == null ? "" : id;
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
               return RedirectToAction("AccessDenied", "Error");
            }
        }
        #endregion

        #region DiárioProjetos
        public IActionResult DiarioProjeto(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 19);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProjectNo = id ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        
        public IActionResult AutorizacaoFaturacao(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 22);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProjectNo = id ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        #endregion

        #region Contratos
        public IActionResult Contratos(int? archived, string contractNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 2);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.Archived = archived == null ? 0 : 1;
                ViewBag.ContractNo = contractNo ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult DetalhesContrato(string id, string version = "")
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 2);
            if (UPerm != null && UPerm.Read.Value)
            {
                Contratos cContract = null;
                if (version != "")
                    cContract = DBContracts.GetByIdAndVersion(id, int.Parse(version));
                else
                    cContract = DBContracts.GetByIdLastVersion(id);

                if (cContract != null && cContract.Arquivado == true)
                {
                    UPerm.Update = false;
                }

                ViewBag.ContractNo = id ?? "";
                ViewBag.VersionNo = version ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        #endregion

        #region Oportunidades
        public IActionResult Oportunidades(int? archived, string contractNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 20);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.Archived = archived == null ? 0 : 1;
                ViewBag.ContractNo = contractNo ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult DetalhesOportunidade(string id, string version = "")
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 20);
            if (UPerm != null && UPerm.Read.Value)
            {
                Contratos cContract = null;
                if (version != "")
                    cContract = DBContracts.GetByIdAndVersion(id, int.Parse(version));
                else
                    cContract = DBContracts.GetByIdLastVersion(id);

                if (cContract != null && cContract.Arquivado == true)
                {
                    UPerm.Update = false;
                }

                ViewBag.ContractNo = id ?? "";
                ViewBag.VersionNo = version ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        #endregion

        #region Propostas
        public IActionResult Propostas(int? archived, string contractNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 21);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.Archived = archived == null ? 0 : 1;
                ViewBag.ContractNo = contractNo ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult DetalhesProposta(string id, string version = "")
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 21);
            if (UPerm != null && UPerm.Read.Value)
            {
                Contratos cContract = null;
                if (version != "")
                    cContract = DBContracts.GetByIdAndVersion(id, int.Parse(version));
                else
                    cContract = DBContracts.GetByIdLastVersion(id);

                if (cContract != null && cContract.Arquivado == true)
                {
                    UPerm.Update = false;
                }

                ViewBag.ContractNo = id ?? "";
                ViewBag.VersionNo = version ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        #endregion
        
        public IActionResult Requisicoes()
        {
            return View();
        }

        public IActionResult FichasTecnicasPratos()
        {
            return View();
        }

        public IActionResult Administracao()
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 18);
            if (UPerm != null && UPerm.Read.Value)
            {
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        #region Folha De Horas
        public IActionResult FolhaDeHoras(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult FolhaDeHoras_Index(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult FolhaDeHoras_IntegracaoAjudaCusto(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult FolhaDeHoras_IntegracaoKMS(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult FolhaDeHoras_Validacao(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult FolhaDeHoras_Historico(string folhaDeHoraNo)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 6);

            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.FolhaDeHorasNo = folhaDeHoraNo ?? "";
                ViewBag.UPermissions = UPerm;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        #endregion

        #region DiárioCafeterias

        public IActionResult DiarioCafeterias(int NºUnidadeProdutiva, int CódigoCafetaria)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 36);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.UPermissions = UPerm;
                ViewBag.CoffeeShopNo = CódigoCafetaria;
                ViewBag.ProdutiveUnityNo = NºUnidadeProdutiva;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public JsonResult GetCoffeShopDiaryDetails([FromBody] CoffeeShopDiaryViewModel data)
        {   
            try
            {
                CoffeeShopDiaryViewModel coffeShopPar = new CoffeeShopDiaryViewModel();
                if (data != null)
                { 
                    coffeShopPar.DateToday = DateTime.Today.ToString("yyyy-MM-dd");
                    coffeShopPar.CoffeShopCode = data.CoffeShopCode;
                    coffeShopPar.ProdutiveUnityNo = data.ProdutiveUnityNo;
                }
                else
                {
                    coffeShopPar.DateToday = DateTime.Today.ToString("yyyy-MM-dd");
                }

                return Json(coffeShopPar);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public JsonResult GetCoffeShopDiary([FromBody] CoffeeShopDiaryViewModel data)
        {
            try
            {
                List<DiárioCafetariaRefeitório> CoffeeShopDiaryList;

                if (data != null)
                {
                    CoffeeShopDiaryList = DBCoffeeShopsDiary.GetByIdsList((int)data.ProdutiveUnityNo,(int)data.CoffeShopCode, User.Identity.Name);

                    List<CoffeeShopDiaryViewModel> result = new List<CoffeeShopDiaryViewModel>();
                    CoffeeShopDiaryList.ForEach(x => result.Add(DBCoffeeShopsDiary.ParseToViewModel(x)));

                    return Json(result);
                }

                return Json(false);
            }
            catch (Exception ex)
            {
                return null;
            } 
        }

        public JsonResult CreateCoffeeShopsDiary([FromBody] CoffeeShopDiaryViewModel data)
        {
            try
            {
                if (data != null)
                {
                    DiárioCafetariaRefeitório newline = new DiárioCafetariaRefeitório();
                    newline = DBCoffeeShopsDiary.ParseToDB(data);
                    newline.Utilizador = User.Identity.Name;
                    newline.UtilizadorCriação = User.Identity.Name;
                    DBCoffeeShopsDiary.Create(newline);

                    if(newline.NºLinha > 0)
                    {
                        return Json(true);
                    }
                    return Json(false);
                }

                return Json(false);
            }
            catch (Exception ex)
            {
                return Json(false);
            }
        }

        public JsonResult DeleteCoffeeShopsDiaryLine([FromBody] CoffeeShopDiaryViewModel data)
        {
            try
            {
                if (data != null)
                {
                    DiárioCafetariaRefeitório lineToRemove = new DiárioCafetariaRefeitório();
                    lineToRemove = DBCoffeeShopsDiary.GetById(data.LineNo);

                    DBCoffeeShopsDiary.Delete(lineToRemove);
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
            catch (Exception ex)
            {

                return Json(false);
            }
        }

        public JsonResult UpdateCoffeeShopsDiaryLine([FromBody] List<CoffeeShopDiaryViewModel> data)
        {
            try
            {
                if (data != null)
                {
                    List<DiárioCafetariaRefeitório> linesToUpdate = new List<DiárioCafetariaRefeitório>();
                    linesToUpdate = DBCoffeeShopsDiary.ParseToDBList(data);
                    linesToUpdate.ForEach(x => DBCoffeeShopsDiary.Update(x));
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
            catch (Exception ex)
            {

                return Json(false);
            }
        }

        public JsonResult CoffeeShopsDiaryLineRegister([FromBody] List<CoffeeShopDiaryViewModel> data)
        {
            try
            {
                if (data != null)
                {
                    int? id = data.Find(x => x.User == User.Identity.Name).CoffeShopCode;
                    CafetariasRefeitórios CoffeeShop = DBCoffeeShops.GetByCode((int)id);

                    foreach (var linesToRegist in data)
                    {
                        MovimentosCafetariaRefeitório MovementsToCreate = new MovimentosCafetariaRefeitório();
                        MovementsToCreate.CódigoCafetariaRefeitório = linesToRegist.CoffeShopCode;
                        MovementsToCreate.NºUnidadeProdutiva = linesToRegist.ProdutiveUnityNo;
                        MovementsToCreate.DataRegisto = linesToRegist.RegistryDate != "" ? DateTime.Parse(linesToRegist.RegistryDate) : (DateTime?)null;
                        MovementsToCreate.NºRecurso = linesToRegist.ResourceNo;
                        MovementsToCreate.Descrição = linesToRegist.Description;
                        MovementsToCreate.Tipo = CoffeeShop.Tipo;
                        if (linesToRegist.MovementType == 2 || linesToRegist.MovementType == 3)
                        {
                            MovementsToCreate.Valor = linesToRegist.Value * (-1);
                        }
                        else
                        {
                            MovementsToCreate.Valor = linesToRegist.Value;
                        }
                        
                        MovementsToCreate.TipoMovimento = linesToRegist.MovementType;
                        MovementsToCreate.CódigoRegião = CoffeeShop.CódigoRegião ?? "";
                        MovementsToCreate.CódigoÁreaFuncional = CoffeeShop.CódigoÁreaFuncional ?? "";
                        MovementsToCreate.CódigoCentroResponsabilidade = CoffeeShop.CódigoCentroResponsabilidade ?? "";
                        MovementsToCreate.Utilizador = User.Identity.Name;
                        MovementsToCreate.DataHoraSistemaRegisto = DateTime.Now;
                        MovementsToCreate.DataHoraCriação = DateTime.Now;
                        MovementsToCreate.UtilizadorCriação = User.Identity.Name;

                        DBCoffeeShopMovements.Create(MovementsToCreate);
                        if(MovementsToCreate.NºMovimento > 0)
                        {
                            DiárioCafetariaRefeitório lineToRemove = new DiárioCafetariaRefeitório();
                            lineToRemove = DBCoffeeShopsDiary.GetById(linesToRegist.LineNo);
                            DBCoffeeShopsDiary.Delete(lineToRemove);
                        }
                    }

                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
            catch (Exception ex)
            {

                return Json(false);
            }
        }
        #endregion

        #region Movimento Produtos

        public IActionResult MovimentoProdutos(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 19);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProjectNo = id ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public JsonResult GetMovementProduct()
        {
            List<ProductMovementViewModel> result = DBProductMovement.ParseToViewModel( DBProductMovement.GetAll());
            return Json(result);
        }


        #endregion

        #region Unidade Medida Produto

        public IActionResult UnidadeMedidaProduto(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 19);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProjectNo = id ?? "";
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public JsonResult GetUnitOfMeasure()
        {
            List<UnitMeasureProductViewModel> result = DBUnitMeasureProduct.ParseToViewModel(DBUnitMeasureProduct.GetAll());
            return Json(result);
        }

        public JsonResult CreateUnitOfMeasure([FromBody] UnitMeasureProductViewModel data)
        {
            string eReasonCode = "";
            //Create new 
            eReasonCode = DBUnitMeasureProduct.Create(DBUnitMeasureProduct.ParseToDb(data)) == null ? "101" : "";
              
            if (String.IsNullOrEmpty(eReasonCode))
            {
                return Json(data);
            }
            else
            {
                return Json(eReasonCode);
            }
        }

        public JsonResult UpdateUnitOfMeasure([FromBody] List<UnitMeasureProductViewModel> data)
        {
            List<UnidadeMedidaProduto> results = DBUnitMeasureProduct.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código && u.ProductNo == x.NºProduto));
            results.ForEach(x => DBUnitMeasureProduct.Delete(x));
            data.ForEach(x =>
            {              
               DBUnitMeasureProduct.Update(DBUnitMeasureProduct.ParseToDb(x));
            });
            return Json(data);
        }

        #endregion

        #region Unidade de Armazenamento

        public IActionResult UnidadeArmazenamento()
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 19);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult UnidadeArmazenamentoDetalhes(string id)
        {
            UserAccessesViewModel UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 3, 19);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.ProductNo = id ?? "";
                if (ViewBag.ProductNo != "")
                {
                    ViewBag.NoProductDisable = true;
                    ViewBag.ButtonHide = 0;
                }
                else
                {
                    ViewBag.NoProductDisable = false;
                    ViewBag.ButtonHide = 1;
                }

                ViewBag.UPermissions = UPerm;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public JsonResult GetUnitStockeeping()
        {
            List<StockkeepingUnitViewModel> result = DBStockkeepingUnit.ParseToViewModel(DBStockkeepingUnit.GetAll());
            return Json(result);
        }

        public JsonResult GetUnitStockeepingId([FromBody]string id)
        {
            StockkeepingUnitViewModel result = DBStockkeepingUnit.ParseToViewModel(DBStockkeepingUnit.GetById(id));
            return Json(result);
        }

        [HttpPost]

        public JsonResult GetProductId([FromBody]string idProduct)
        {
            List<NAVProductsViewModel> product = DBNAV2017Products.GetAllProducts(_config.NAVDatabaseName, _config.NAVCompanyName, idProduct);
            return Json(product);
        }


        public JsonResult CreateUnitStockeeping([FromBody] StockkeepingUnitViewModel data)
        {
            string eReasonCode = "";
            //Update 
            eReasonCode = DBStockkeepingUnit.Create(DBStockkeepingUnit.ParseToDb(data)) == null ? "101" : "";

            if (String.IsNullOrEmpty(eReasonCode))
            {
                return Json(data);
            }
            else
            {
                return Json(eReasonCode);
            }
        }

        public JsonResult DeleteUnitStockeeping([FromBody] StockkeepingUnitViewModel data)
        {
           
            string eReasonCode = "";
            //Create new 
            eReasonCode = DBStockkeepingUnit.Delete(DBStockkeepingUnit.ParseToDb(data)) == true ? "103" : "";
            
            if (String.IsNullOrEmpty(eReasonCode))
            {
                return Json(null);
            }
            else
            {
                return Json(eReasonCode);
            }
        }

        public JsonResult  UpdateUnitStockeeping([FromBody] StockkeepingUnitViewModel data)
        {
            string eReasonCode = "";
            //Create new 
            eReasonCode = DBStockkeepingUnit.Update(DBStockkeepingUnit.ParseToDb(data)) == null ? "102" : "";

            if (String.IsNullOrEmpty(eReasonCode))
            {
                return Json(data);
            }
            else
            {
                return Json(eReasonCode);
            }
    

         
        }
        #endregion
    }
}