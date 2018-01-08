﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hydra.Such.Portal.Configurations;
using Hydra.Such.Data.Logic.Request;
using Hydra.Such.Data.Logic.Compras;
using Hydra.Such.Data.NAV;
using Hydra.Such.Data.ViewModel;
using Hydra.Such.Data.ViewModel.Compras;

namespace Hydra.Such.Portal.Services
{
    public class CreatePrePurchOrderResult
    {
        public bool CompletedSuccessfully { get; set; }
        public string NAVPrePurchOrderId { get; set; }
        public string ErrorMessage { get; set; }

        public CreatePrePurchOrderResult()
        {
            this.CompletedSuccessfully = false;
        }
    }

    public class RequisitionService
    {
        private readonly NAVWSConfigurations _configws;

        public RequisitionService(NAVWSConfigurations NAVWSConfigs)
        {
            _configws = NAVWSConfigs;
        }

        public RequisitionViewModel ValidateRequisition(RequisitionViewModel requisition, string validatedByUserName)
        {
            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0 && requisition.State == RequisitionStates.Approved)
            {
                var linesToValidate = requisition.Lines
                    .Where(x => x.QuantityRequired != null && x.QuantityRequired.Value > 0)
                    .ToList();

                if (linesToValidate.Count() > 0)
                {
                    requisition.State = RequisitionStates.Validated;
                    requisition.ResponsibleValidation = validatedByUserName;
                    requisition.ValidationDate = DateTime.Now;

                    linesToValidate.ForEach(item =>
                        item.QuantityToProvide = item.QuantityRequired
                    );

                    var updatedReq = DBRequest.UpdateHeaderAndLines(requisition.ParseToDB());
                    if (updatedReq != null)
                    {
                        requisition = updatedReq.ParseToViewModel();
                        requisition.eReasonCode = 1;
                        requisition.eMessage = "Requisição validada com sucesso.";
                    }
                    else
                    {
                        requisition.eReasonCode = 3;
                        requisition.eMessage = "Ocorreu um erro ao validar a requisição.";
                    }
                }
                else
                {
                    requisition.eReasonCode = 3;
                    requisition.eMessage = "Não existem linhas que cumpram os requisitos de validação.";
                }
            }
            else
            {
                requisition = new RequisitionViewModel()
                {
                    eReasonCode = 3,
                    eMessage = " O estado da requisição e / ou linhas não cumprem os requisitos de validação.",
                };
            }
            return requisition;
        }

        public ErrorHandler CreatePrePurchaseOrderFor(RequisitionViewModel requisition)
        {
            ErrorHandler status = new ErrorHandler();
            
            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0 && requisition.State == RequisitionStates.Approved)
            {
                /*
                    Filtrar as linhas da requisição cujos campos ‘Mercado Local’ seja = true, ‘Validado Compras’=false e ‘Quandidade Requerida’ > 0;
                    18-12-2017: Indicação para agrupar por fornecedor para criação de cabeçalhos e linhas na tab. Compras do NAV.
                */
                //use for database update later
                var requisitionLines = requisition.Lines
                    .Where(x =>
                        x.LocalMarket != null
                        && x.PurchaseValidated != null
                        && x.QuantityRequired != null
                        && x.LocalMarket.Value
                        && !x.PurchaseValidated.Value
                        && x.QuantityRequired.Value > 0)
                    .ToList();

                var purchOrders = requisitionLines.GroupBy(x =>
                            x.SupplierNo,
                            x => x,
                            (key, items) => new PurchOrderDTO
                            {
                                SupplierId = key,
                                RequisitionId = requisition.RequisitionNo,
                                CenterResponsibilityCode = requisition.CenterResponsibilityCode,
                                FunctionalAreaCode = requisition.FunctionalAreaCode,
                                RegionCode = requisition.RegionCode,
                                Lines = items.Select(line => new PurchOrderLineDTO()
                                {
                                    LineId = line.LineNo.Value,
                                    Type = line.Type,
                                    Code = line.Code,
                                    Description = line.Description,
                                    ProjectNo = line.ProjectNo,
                                    QuantityRequired = line.QuantityRequired,
                                    UnitCost = line.UnitCost,
                                    LocationCode = line.LocalCode,
                                    OpenOrderNo = line.OpenOrderNo,
                                    OpenOrderLineNo = line.OpenOrderLineNo
                                })
                                .ToList()
                            })
                    .ToList();

                if (purchOrders.Count() > 0)
                {
                    string executionReport = "Relatório de validação de mercado local: ";
                    bool hasErros = false;
                    purchOrders.ForEach(purchOrder =>
                    {
                        Task<WSPurchaseInvHeader.Create_Result> createPurchaseHeaderTask = NAVPurchaseHeaderService.CreateAsync(purchOrder, _configws);
                        try
                        {
                            createPurchaseHeaderTask.Wait();
                            if (createPurchaseHeaderTask.IsCompletedSuccessfully)
                            {
                                purchOrder.NAVPrePurchOrderId = createPurchaseHeaderTask.Result.WSPurchInvHeaderInterm.No;

                                executionReport += string.Format("Criada a pré-compra {0}.", purchOrder.NAVPrePurchOrderId);
                                Task<WSPurchaseInvLine.CreateMultiple_Result> createPurchaseLinesTask = NAVPurchaseLineService.CreateMultipleAsync(purchOrder, _configws);
                                try
                                {
                                    createPurchaseLinesTask.Wait();
                                    if (createPurchaseLinesTask.IsCompletedSuccessfully)
                                    {
                                        executionReport += string.Format(" Criadas linhas de pré-compra com sucesso.");
                                    }
                                    else
                                    {
                                        executionReport += string.Format(" Não foi possivel criar as linhas de pré-compra.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    hasErros = true;
                                    executionReport += string.Format(" Ocorreu um erro ao criar as linhas de pré-compra no NAV.");
                                }
                            }
                            else
                            {
                                executionReport += string.Format(" Ocorreu um erro ao criar a pré-compra para o fornecedor com o ID:{0}.", purchOrder.SupplierId);
                            }
                        }
                        catch (Exception ex)
                        {
                            hasErros = true;
                            executionReport += string.Format(" Ocorreu um erro ao criar a pré-compra no NAV.");
                        }
                    });
                    status.eReasonCode = hasErros ? 2 : 1;
                    status.eMessage = executionReport;
                }
                else
                {
                    status.eReasonCode = 3;
                    status.eMessage = " Não existem linhas que cumpram os requisitos de validação.";
                }
            }
            else
            {
                status.eReasonCode = 3;
                status.eMessage = " O estado da requisição e / ou linhas não cumprem os requisitos de validação.";
            }
            return status;
        }
        
        public ErrorHandler CreateMarketConsultFor(RequisitionViewModel requisition)
        {
            throw new NotImplementedException("CreateMarketConsultFor");
        }

        //public ErrorHandler CreatePurchaseOrderFor(RequisitionViewModel requisition)
        //{
        //    ErrorHandler status = new ErrorHandler();

        //    if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0)
        //    {
        //        //use for database update later
        //        var requisitionLines = requisition.Lines;

        //        var purchOrders = requisitionLines.GroupBy(x =>
        //                    x.SupplierNo,
        //                    x => x,
        //                    (key, items) => new PurchOrderDTO
        //                    {
        //                        SupplierId = key,
        //                        RequisitionId = requisition.RequisitionNo,
        //                        CenterResponsibilityCode = requisition.CenterResponsibilityCode,
        //                        FunctionalAreaCode = requisition.FunctionalAreaCode,
        //                        RegionCode = requisition.RegionCode,
        //                        Lines = items.Select(line => new PurchOrderLineDTO()
        //                        {
        //                            LineId = line.LineNo.Value,
        //                            Type = line.Type,
        //                            Code = line.Code,
        //                            Description = line.Description,
        //                            ProjectNo = line.ProjectNo,
        //                            QuantityRequired = line.QuantityRequired,
        //                            UnitCost = line.UnitCost,
        //                            LocationCode = line.LocalCode,
        //                            OpenOrderNo = line.OpenOrderNo,
        //                            OpenOrderLineNo = line.OpenOrderLineNo
        //                        })
        //                        .ToList()
        //                    })
        //            .ToList();

        //        if (purchOrders.Count() > 0)
        //        {
        //            string executionReport = "Relatório: ";
        //            bool hasErros = false;
        //            purchOrders.ForEach(purchOrder =>
        //            {
        //                try
        //                {
        //                    Task<WSPurchaseInvHeader.Create_Result> createPurchaseHeaderTask = NAVPurchaseHeaderService.CreateAsync(purchOrder, _configws);
        //                    createPurchaseHeaderTask.Wait();
        //                    if (createPurchaseHeaderTask.IsCompletedSuccessfully)
        //                    {
        //                        purchOrder.NAVPrePurchOrderId = createPurchaseHeaderTask.Result.WSPurchInvHeaderInterm.No;

        //                        try
        //                        {
        //                            Task<WSPurchaseInvLine.CreateMultiple_Result> createPurchaseLinesTask = NAVPurchaseLineService.CreateMultipleAsync(purchOrder, _configws);
        //                            createPurchaseLinesTask.Wait();
        //                            if (createPurchaseLinesTask.IsCompletedSuccessfully)
        //                            {
        //                                try
        //                                {
        //                                    Task<WSGenericCodeUnit.FxCabimento_Result> createPurchOrderTask = WSGeneric.CreatePurchaseOrderFitting(purchOrder.NAVPrePurchOrderId, _configws);
        //                                    createPurchOrderTask.Wait();
        //                                    if (createPurchOrderTask.IsCompletedSuccessfully)
        //                                    {
                                                
        //                                        //TODO: As linhas da requisição devem ficar com a informação do nº da encomeda compromisso e nº encomenda cabimento; O Nº da requisição e linha deverão passar para a linha da encomenda compromisso
        //                                        //Get id's from NAV. createPurchOrderTask.Result...
        //                                        string tempPurchOrderFitId = "";
        //                                        //string tempPurchOrderCommitmentId = "";
        //                                        //var linesToUpdate = linesToCreateFrom.Where(x => purchFromSupplier.Lines.Select(y => y.LineId).ToArray().Contains(x.LineNo.Value)).ToList();

        //                                        //purchFromSupplier.Lines.ForEach(line =>
        //                                        //{
        //                                        //    var lineToUpdate = linesToUpdate.FirstOrDefault(x => x.LineNo == line.LineId);
        //                                        //    if (lineToUpdate != null)
        //                                        //    {
        //                                        //        lineToUpdate.PurchOrderFitId = tempPurchOrderFitId;
        //                                        //        lineToUpdate.PurchOrderCommitmentId = tempPurchOrderCommitmentId;
        //                                        //    }
        //                                        //});
        //                                        //UpdateLines(linesToUpdate);
        //                                    }
        //                                    else
        //                                    {

        //                                    }
        //                                }
        //                                catch
        //                                {

        //                                }
        //                            }
        //                            else
        //                            {
        //                                executionReport += string.Format(" Não foi possivel criar as linhas de pré-compra.");
        //                            }
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            hasErros = true;
        //                            executionReport += string.Format(" Ocorreu um erro ao criar as linhas de pré-compra no NAV.");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        executionReport += string.Format("Ocorreu um erro ao criar a pré-compra para o fornecedor com o ID:{0}.", purchOrder.SupplierId);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    hasErros = true;
        //                    executionReport += string.Format(" Ocorreu um erro ao criar a pré-compra no NAV.");
        //                }
        //            });
        //            status.eReasonCode = hasErros ? 2 : 1;
        //            status.eMessage = executionReport;
        //        }
        //        else
        //        {
        //            status.eReasonCode = 3;
        //            status.eMessage = "Não existem linhas que cumpram os requisitos de validação do mercado local.";
        //        }
        //    }
        //    return status;
        //}
        
        public ErrorHandler CreatePurchaseOrderFor(RequisitionViewModel requisition)
        {
            ErrorHandler status = new ErrorHandler();

            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0)
            {
                //use for database update later
                var requisitionLines = requisition.Lines;

                List<PurchOrderDTO> purchOrders = new List<PurchOrderDTO>();

                try
                {
                    purchOrders = requisitionLines.GroupBy(x =>
                                x.SupplierNo,
                                x => x,
                                (key, items) => new PurchOrderDTO
                                {
                                    SupplierId = key,
                                    RequisitionId = requisition.RequisitionNo,
                                    CenterResponsibilityCode = requisition.CenterResponsibilityCode,
                                    FunctionalAreaCode = requisition.FunctionalAreaCode,
                                    RegionCode = requisition.RegionCode,
                                    Lines = items.Select(line => new PurchOrderLineDTO()
                                    {
                                        LineId = line.LineNo,
                                        Type = line.Type,
                                        Code = line.Code,
                                        Description = line.Description,
                                        ProjectNo = line.ProjectNo,
                                        QuantityRequired = line.QuantityRequired,
                                        UnitCost = line.UnitCost,
                                        LocationCode = line.LocalCode,
                                        OpenOrderNo = line.OpenOrderNo,
                                        OpenOrderLineNo = line.OpenOrderLineNo
                                    })
                                    .ToList()
                                })
                        .ToList();
                }
                catch
                {
                    throw new Exception("Ocorreu um erro ao agrupar os items.");
                }

                if (purchOrders.Count() > 0)
                {
                    purchOrders.ForEach(purchOrder =>
                    {
                        try
                        {
                            var result = CreateNAVPrePurchaseOrderFor(purchOrder);
                            if (result.CompletedSuccessfully)
                            {
                                //Update Requisition Lines
                                requisition.Lines.ForEach(line =>
                                   line.CreatedOrderNo = result.NAVPrePurchOrderId);

                                bool linesUpdated = DBRequestLine.Update(requisition.Lines.ParseToDB());
                                if (linesUpdated)
                                {
                                    status.eMessages.Add(new TraceInformation(TraceType.Success, "Criada encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                                }
                            }
                        }
                        catch
                        {
                            status.eMessages.Add(new TraceInformation(TraceType.Error, "Ocorreu um erro ao criar encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                        }
                    });
                    status.eReasonCode = status.eMessages.Any(x => x.Type == TraceType.Error) ? 2 : 1;
                }
                else
                {
                    status.eReasonCode = 3;
                    //status.eMessage = "Não existem linhas que cumpram os requisitos de validação do mercado local.";
                    status.eMessages.Add(new TraceInformation(TraceType.Error, "Não existem linhas que cumpram os requisitos de validação do mercado local."));
                }
            }
            return status;
        }

        public ErrorHandler CreateTransferShipmentFor(RequisitionViewModel requisition)
        {
            throw new NotImplementedException("CreatePurchaseOrderCommitmentFrom");
        }

        public ErrorHandler SendPrePurchaseFor(RequisitionViewModel requisition, string createdByUserName)
        {
            ErrorHandler status = new ErrorHandler();

            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0 && requisition.State == RequisitionStates.Validated)
            {
                //use for later database update
                var requisitionLines = requisition.Lines
                    .Where(x =>
                        x.SubmitPrePurchase != null
                        && x.SubmitPrePurchase.Value)
                    .ToList();

                var prePurchOrderLines = requisitionLines
                    .Select(line => new PrePurchOrderLineViewModel()
                    {
                        RequisitionNo = line.RequestNo,
                        RequisitionLineNo = line.LineNo,
                        ProductCode = line.Code,
                        ProductDescription = line.Description,
                        UnitOfMeasureCode = line.UnitMeasureCode,
                        LocationCode = line.LocalCode,
                        QuantityAvailable = line.QuantityAvailable,
                        UnitCost = line.UnitCost,
                        ProjectNo = line.ProjectNo,
                        RegionCode = line.RegionCode,
                        FunctionalAreaCode = line.FunctionalAreaCode,
                        CenterResponsibilityCode = line.CenterResponsibilityCode,
                        CreateUser = createdByUserName,
                        SupplierNo = line.SupplierNo,
                    })
                    .ToList();

                if (prePurchOrderLines.Count() > 0)
                {    
                    try
                    {
                        var createdLines = DBPrePurchOrderLines.Create(prePurchOrderLines.ParseToDB());
                        if (createdLines != null)
                        {
                            //Update Requisition Lines
                            requisitionLines.ForEach(line =>
                               line.SubmitPrePurchase = true);

                            bool linesUpdated = DBRequestLine.Update(requisitionLines.ParseToDB());
                            if (linesUpdated)
                            {
                                status.eReasonCode = 1;
                                status.eMessage = "Pré-Encomenda enviada com sucesso";
                            }
                        }
                    }
                    catch
                    {
                        status.eReasonCode = 2;
                        status.eMessage = "Ocorreu um erro ao enviar a pré-encomenda.";
                    }
                }
                else
                {
                    status.eReasonCode = 2;
                    status.eMessage = " Não existem linhas para enviar.";
                }
            }
            else
            {
                status.eReasonCode = 2;
                status.eMessage = " O estado da requisição e / ou linhas não cumprem os requisitos.";
            }
            return status;
        }

        private CreatePrePurchOrderResult CreateNAVPrePurchaseOrderFor(PurchOrderDTO purchOrder)
        {
            CreatePrePurchOrderResult result = new CreatePrePurchOrderResult();

            Task<WSPurchaseInvHeader.Create_Result> createPurchaseHeaderTask = NAVPurchaseHeaderService.CreateAsync(purchOrder, _configws);
            createPurchaseHeaderTask.Wait();
            if (createPurchaseHeaderTask.IsCompletedSuccessfully)
            {
                result.NAVPrePurchOrderId = createPurchaseHeaderTask.Result.WSPurchInvHeaderInterm.No;

                Task<WSPurchaseInvLine.CreateMultiple_Result> createPurchaseLinesTask = NAVPurchaseLineService.CreateMultipleAsync(purchOrder, _configws);
                createPurchaseLinesTask.Wait();
                if (createPurchaseLinesTask.IsCompletedSuccessfully)
                {
                    Task<WSGenericCodeUnit.FxCabimento_Result> createPurchOrderTask = WSGeneric.CreatePurchaseOrderFitting(purchOrder.NAVPrePurchOrderId, _configws);
                    createPurchOrderTask.Wait();
                    if (createPurchOrderTask.IsCompletedSuccessfully)
                    {
                        result.CompletedSuccessfully = true;
                    }
                }
            }
            return result;
        }
    }
}
