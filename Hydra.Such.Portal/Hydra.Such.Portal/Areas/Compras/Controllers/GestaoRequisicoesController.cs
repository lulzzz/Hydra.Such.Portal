﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hydra.Such.Data.Database;
using Hydra.Such.Data.Logic;
using Hydra.Such.Data.Logic.Request;
using Hydra.Such.Data.ViewModel.Compras;
using Microsoft.AspNetCore.Mvc;
using Hydra.Such.Data.ViewModel;
using Hydra.Such.Data.NAV;
using Hydra.Such.Portal.Configurations;
using Microsoft.Extensions.Options;

namespace Hydra.Such.Portal.Areas.Compras.Controllers
{
    public class GestaoRequisicoesController : Controller
    {
        private readonly NAVConfigurations _config;
        private readonly NAVWSConfigurations _configws;

        public GestaoRequisicoesController(IOptions<NAVConfigurations> appSettings, IOptions<NAVWSConfigurations> NAVWSConfigs)
        {
            _config = appSettings.Value;
            _configws = NAVWSConfigs.Value;
        }

        [Area("Compras")]
        public IActionResult Index()
        {
            return View();
        }

        [Area("Compras")]
        public IActionResult Detalhes()
        {
            UserAccessesViewModel userPermissions =
                DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 10, 4);
            if (userPermissions != null && userPermissions.Read.Value)
            {
                ViewBag.UPermissions = userPermissions;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [Area("Compras")]
        public IActionResult RequisicoesAprovadas()
        {
            return View();
        }

        [Area("Compras")]
        public IActionResult DetalhesReqAprovada(string id)
        {
            UserAccessesViewModel userPermissions = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 10, 4);
            
            
            if (userPermissions != null && userPermissions.Read.Value)
            {
                ViewBag.UPermissions = userPermissions;
                ViewBag.RequisitionId = id;
                ViewBag.RequisitionStatesEnumString = EnumHelper.GetItemsAsDictionary(typeof(RequisitionStates));

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [Area("Compras")]
        public IActionResult LinhasRequisicao(string id)
        {

            UserAccessesViewModel userPermissions = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, 10, 4);

            if (userPermissions != null && userPermissions.Read.Value)
            {
                ViewBag.UPermissions = userPermissions;
                ViewBag.RequisitionId = id;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        [Area("Compras")]
        public JsonResult GridRequestLineValues([FromBody] string id)
        {
            List<RequisitionLineViewModel> result = DBRequestLine.GetAllByRequisiçãos(id).ParseToViewModel();
            //Apply User Dimensions Validations
            List<AcessosDimensões> CUserDimensions = DBUserDimensions.GetByUserId(User.Identity.Name);
            //Regions
            if (CUserDimensions.Where(y => y.Dimensão == 1).Count() > 0)
                result.RemoveAll(x => !CUserDimensions.Any(y => y.Dimensão == 1 && y.ValorDimensão == x.RegionCode));
            //FunctionalAreas
            if (CUserDimensions.Where(y => y.Dimensão == 2).Count() > 0)
                result.RemoveAll(x => !CUserDimensions.Any(y => y.Dimensão == 2 && y.ValorDimensão == x.FunctionalAreaCode));
            //ResponsabilityCenter
            if (CUserDimensions.Where(y => y.Dimensão == 3).Count() > 0)
                result.RemoveAll(x => !CUserDimensions.Any(y => y.Dimensão == 3 && y.ValorDimensão == x.CenterResponsibilityCode));
            return Json(result);
        }

        [HttpPost]
        [Area("Compras")]
        public JsonResult CreateAndDeleteRequestLine([FromBody] List<RequisitionLineViewModel> dp)
        {
            List<LinhasRequisição> previousList;
            // Get All
            previousList = DBRequestLine.GetAll();
            foreach (LinhasRequisição line in previousList)
            {
                if (!dp.Any(x => x.LineNo == line.NºLinha))
                {
                    DBRequestLine.Delete(line);
                }
            }
            //Update or Create
            try
            {
                dp.ForEach(x =>
                {
                    List<LinhasRequisição> dpObject = DBRequestLine.GetByLineNo((int)x.LineNo);

                    if (dpObject.Count > 0)
                    {
                        LinhasRequisição newdp = dpObject.FirstOrDefault();
                          newdp.NºRequisição= x.RequestNo;
                           newdp.NºLinha = (int) x.LineNo;
                            newdp.Tipo = 2;
                          newdp.Código= x.Code;
                         newdp.Descrição = x.Description;
                          newdp.CódigoUnidadeMedida= x.UnitMeasureCode;
                          newdp.CódigoLocalização= x.LocalCode;
                          newdp.MercadoLocal= x.LocalMarket;
                          newdp.QuantidadeARequerer= x.QuantityToRequire;
                          newdp.QuantidadeRequerida= x.QuantityRequired;
                          newdp.QuantidadeADisponibilizar= x.QuantityToProvide;
                         newdp.QuantidadeDisponibilizada= x.QuantityAvailable;
                          newdp.QuantidadeAReceber= x.QuantityReceivable;
                          newdp.QuantidadeRecebida= x.QuantityReceived;
                          newdp.QuantidadePendente= x.QuantityPending;
                          newdp.CustoUnitário= x.UnitCost;
                          newdp.DataReceçãoEsperada= string.IsNullOrEmpty(x.ExpectedReceivingDate) ? (DateTime?)null : DateTime.Parse(x.ExpectedReceivingDate) ;
                        newdp.Faturável = x.Billable;
                          newdp.NºProjeto= x.ProjectNo;
                          newdp.CódigoRegião= x.RegionCode;
                          newdp.CódigoÁreaFuncional= x.FunctionalAreaCode;
                          newdp.CódigoCentroResponsabilidade= x.CenterResponsibilityCode;
                           newdp.NºFuncionário  = x.FunctionalNo;
                           newdp.Viatura  = x.Vehicle;
                           newdp.DataHoraCriação  = x.CreateDateTime;
                          newdp.UtilizadorCriação = x.CreateUser;
                        newdp.DataHoraModificação  = x.UpdateDateTime;
                        newdp.UtilizadorModificação  = x.UpdateUser;
                          newdp.QtdPorUnidadeDeMedida  = x.QtyByUnitOfMeasure;
                         newdp.PreçoUnitárioVenda  = x.UnitCostsould;
                          newdp.ValorOrçamento  = x.BudgetValue;
                          newdp.NºLinhaOrdemManutenção  = x.MaintenanceOrderLineNo;
                          newdp.CriarConsultaMercado  = x.CreateMarketSearch;
                          newdp.EnviarPréCompra  = x.SubmitPrePurchase;
                         newdp.EnviadoPréCompra = x.SendPrePurchase;
                          newdp.DataMercadoLocal = string.IsNullOrEmpty(x.LocalMarketDate) ? (DateTime?)null : DateTime.Parse(x.LocalMarketDate) ;
                         newdp.UserMercadoLocal  = x.LocalMarketUser;
                          newdp.EnviadoParaCompras  = x.SendForPurchase;
                          newdp.DataEnvioParaCompras  = string.IsNullOrEmpty(x.SendForPurchaseDate) ? (DateTime?)null : DateTime.Parse(x.SendForPurchaseDate) ;
                          newdp.ValidadoCompras  = x.PurchaseValidated;
                       newdp.RecusadoCompras = x.PurchaseRefused;
                         newdp.MotivoRecusaMercLocal = x.ReasonToRejectionLocalMarket;
                        newdp.DataRecusaMercLocal  = string.IsNullOrEmpty(x.RejectionLocalMarketDate) ? (DateTime?)null : DateTime.Parse(x.RejectionLocalMarketDate) ;
                         newdp.IdCompra = x.PurchaseId;
                        newdp.NºFornecedor = x.SupplierNo;
                        newdp.NºEncomendaAberto  = x.OpenOrderNo;
                          newdp.NºLinhaEncomendaAberto  = x.OpenOrderLineNo;
                        newdp.NºDeConsultaMercadoCriada = x.QueryCreatedMarketNo;
                        newdp.NºEncomendaCriada = x.CreatedOrderNo;
                          newdp.CódigoProdutoFornecedor  = x.SupplierProductCode;
                         newdp.UnidadeProdutivaNutrição  = x.UnitNutritionProduction;
                         newdp.RegiãoMercadoLocal  = x.MarketLocalRegion;
                         newdp.NºCliente  = x.CustomerNo;
                         newdp.Aprovadores  = x.Approvers;
                        DBRequestLine.Update(newdp);
                    }
                    else
                    {
                        LinhasRequisição newdp = new LinhasRequisição()
                        {
                        NºRequisição = x.RequestNo,
                        NºLinha = (int)x.LineNo,
                        Tipo = 2,
                        Código = x.Code,
                        Descrição = x.Description,
                        CódigoUnidadeMedida = x.UnitMeasureCode,
                        CódigoLocalização = x.LocalCode,
                        MercadoLocal = x.LocalMarket,
                        QuantidadeARequerer = x.QuantityToRequire,
                        QuantidadeRequerida = x.QuantityRequired,
                        QuantidadeADisponibilizar = x.QuantityToProvide,
                        QuantidadeDisponibilizada = x.QuantityAvailable,
                        QuantidadeAReceber = x.QuantityReceivable,
                        QuantidadeRecebida = x.QuantityReceived,
                        QuantidadePendente = x.QuantityPending,
                        CustoUnitário = x.UnitCost,
                        DataReceçãoEsperada = string.IsNullOrEmpty(x.ExpectedReceivingDate) ? (DateTime?)null : DateTime.Parse(x.ExpectedReceivingDate),
                        Faturável = x.Billable,
                        NºProjeto = x.ProjectNo,
                        CódigoRegião = x.RegionCode,
                        CódigoÁreaFuncional = x.FunctionalAreaCode,
                        CódigoCentroResponsabilidade = x.CenterResponsibilityCode,
                        NºFuncionário = x.FunctionalNo,
                        Viatura = x.Vehicle,
                        DataHoraCriação = x.CreateDateTime,
                        UtilizadorCriação = x.CreateUser,
                        DataHoraModificação = x.UpdateDateTime,
                        UtilizadorModificação = x.UpdateUser,
                        QtdPorUnidadeDeMedida = x.QtyByUnitOfMeasure,
                        PreçoUnitárioVenda = x.UnitCostsould,
                        ValorOrçamento = x.BudgetValue,
                        NºLinhaOrdemManutenção = x.MaintenanceOrderLineNo,
                        CriarConsultaMercado = x.CreateMarketSearch,
                        EnviarPréCompra = x.SubmitPrePurchase,
                        EnviadoPréCompra = x.SendPrePurchase,
                        DataMercadoLocal = string.IsNullOrEmpty(x.LocalMarketDate) ? (DateTime?)null : DateTime.Parse(x.LocalMarketDate),
                        UserMercadoLocal = x.LocalMarketUser,
                        EnviadoParaCompras = x.SendForPurchase,
                        DataEnvioParaCompras = string.IsNullOrEmpty(x.SendForPurchaseDate) ? (DateTime?)null : DateTime.Parse(x.SendForPurchaseDate) ,
                        ValidadoCompras = x.PurchaseValidated,
                        RecusadoCompras = x.PurchaseRefused,
                        MotivoRecusaMercLocal = x.ReasonToRejectionLocalMarket,
                        DataRecusaMercLocal = string.IsNullOrEmpty(x.RejectionLocalMarketDate) ? (DateTime?)null : DateTime.Parse(x.RejectionLocalMarketDate) ,
                        IdCompra = x.PurchaseId,
                        NºFornecedor = x.SupplierNo,
                        NºEncomendaAberto = x.OpenOrderNo,
                        NºLinhaEncomendaAberto = x.OpenOrderLineNo,
                        NºDeConsultaMercadoCriada = x.QueryCreatedMarketNo,
                        NºEncomendaCriada = x.CreatedOrderNo,
                        CódigoProdutoFornecedor = x.SupplierProductCode,
                        UnidadeProdutivaNutrição = x.UnitNutritionProduction,
                        RegiãoMercadoLocal = x.MarketLocalRegion,
                        NºCliente = x.CustomerNo,
                        Aprovadores = x.Approvers,
                    };
                        newdp.UtilizadorCriação = User.Identity.Name;

                        newdp.DataHoraCriação = DateTime.Now;
                        DBRequestLine.Create(newdp);
                    }
                });
            }
            catch (Exception e)
            {
                return Json(null);
            }
            return Json(dp);
        }

        [HttpPost]
        [Area("Compras")]
        public JsonResult GetApprovedRequisitions()
        {
            List<RequisitionViewModel> result = DBRequest.GetByState(RequisitionStates.Approved).ParseToViewModel();
            
            //Apply User Dimensions Validations
            List<AcessosDimensões> userDimensions = DBUserDimensions.GetByUserId(User.Identity.Name);
            //Regions
            if (userDimensions.Where(y => y.Dimensão == 1).Count() > 0)
                result.RemoveAll(x => !userDimensions.Any(y => y.Dimensão == 1 && y.ValorDimensão == x.RegionCode));
            //FunctionalAreas
            if (userDimensions.Where(y => y.Dimensão == 2).Count() > 0)
                result.RemoveAll(x => !userDimensions.Any(y => y.Dimensão == 2 && y.ValorDimensão == x.FunctionalAreaCode));
            //ResponsabilityCenter
            if (userDimensions.Where(y => y.Dimensão == 3).Count() > 0)
                result.RemoveAll(x => !userDimensions.Any(y => y.Dimensão == 3 && y.ValorDimensão == x.CenterResponsibilityCode));
            return Json(result);
        }
        
        [HttpPost]
        [Area("Compras")]
        public JsonResult GetRequisition([FromBody] Newtonsoft.Json.Linq.JObject requestParams)
        {
            string requisitionId = string.Empty;
            int status = -1;
            
            if (requestParams != null)
            {
                requisitionId = requestParams["requisitionId"].ToString();
                status = int.Parse(requestParams["status"].ToString());
            }

            bool statusIsValid = Configurations.EnumHelper.ValidateRange(typeof(RequisitionStates), status);

            RequisitionViewModel item;
            if (!string.IsNullOrEmpty(requisitionId) && requisitionId != "0" && statusIsValid)
            {
                item = DBRequest.GetById(requisitionId).ParseToViewModel();
            }
            else
                item = new RequisitionViewModel();

            return Json(item);
        }

        [HttpPost]
        [Area("Compras")]
        public JsonResult ValidateLocalMarketForRequisition([FromBody] RequisitionViewModel item)
        {
            //Validate
            if (item != null)
            {
                //Ensure that the requisition has the expected status. Ex.: prevents from validating pending requisitions
                if (item == null || item.State != RequisitionStates.Approved)
                    item = new RequisitionViewModel();
                var status = CreatePurchaseItemsFor(item);
            }
            else
            {
                item = new RequisitionViewModel()
                {
                    eReasonCode = 3,
                    eMessage = "Não é possivel validar o mercado local. A requisição não pode ser nula."
                };
            }
            
            return Json(item);
        }

        private ErrorHandler CreatePurchaseItemsFor(RequisitionViewModel requisition)
        {
            ErrorHandler status = new ErrorHandler();
            if (requisition != null && requisition.Lines.Count > 0)
            {
                /*
                    Filtrar as linhas da requisição cujos campos ‘Mercado Local’ seja = true, ‘Validado Compras’=false e ‘Quandidade Requerida’ > 0;
                */
                var linesToValidate = requisition.Lines.Where(x => x.LocalMarket.Value && !x.PurchaseValidated.Value && x.QuantityRequired.Value > 0);
                if (linesToValidate.Count() > 0)
                {
                    //Task<WSContacts.Create_Result> createPurchasesTask = NAVPurchaseService.CreateAsync(item, _configws);
                    //try
                    //{
                    //    createPurchasesTask.Wait();
                    //}
                    //catch (Exception ex)
                    //{
                    //    item.eReasonCode = 3;
                    //    item.eMessage = "Ocorreu um erro ao criar o contacto no NAV.";
                    //}
                }
                else
                {
                    status.eReasonCode = 3;
                    status.eMessage = "Não existem linhas que cumpram os requisitos de validação do mercado local.";
                }
            }
            return status;
        }

        [HttpPost]
        [Area("Compras")]
        public JsonResult ValidateRequisition([FromBody] RequisitionViewModel item)
        {
            /*
                Header	estado
                Lines	onde ‘Quantidade Requerida’ seja > 0;
	                ‘Quantidade a Disponibilizar’ = Quantidade Requerida’
	                Responsável Validação = utilizador atual
	                Data Validação = now

                	Criar na tabela de workflow de registo de validação e respetiva submissão
             */
            if (item != null)
            {
                //Ensure that the requisition has the expected status. Ex.: prevents from validating pending requisitions
                if (item == null || item.State != RequisitionStates.Approved)
                {
                    item = new RequisitionViewModel();
                    item.eReasonCode = 3;
                    item.eMessage = "O estado da requisição não permite a validação.";
                }
                else
                {
                    var linesToValidate = item.Lines
                            .Where(x => x.QuantityRequired.Value > 0);

                    if (linesToValidate.Count() > 0)
                    {
                        item.State = RequisitionStates.Validated;
                        item.ResponsibleValidation = User.Identity.Name;
                        item.ValidationDate = DateTime.Now;
                        
                        linesToValidate.ToList().ForEach(x =>
                                x.QuantityToProvide = x.QuantityRequired
                            );
                    }
                    else
                    {
                        item.eReasonCode = 3;
                        item.eMessage = "Não existem linhas que cumpram os requisitos de validação.";
                    }
                }
            }
            else
            {
                item = new RequisitionViewModel()
                {
                    eReasonCode = 3,
                    eMessage = "Não é possivel validar. A requisição não pode ser nula."
                };
            }

            return Json(item);
        }
    }
}