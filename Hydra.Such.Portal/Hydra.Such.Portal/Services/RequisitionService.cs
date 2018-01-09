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
        private readonly NAVWSConfigurations configws;
        private readonly string changedByUserName;

        public RequisitionService(NAVWSConfigurations NAVWSConfigs, string logChangesAsUserName)
        {
            this.configws = NAVWSConfigs;
            this.changedByUserName = logChangesAsUserName;
        }

        public RequisitionViewModel ValidateRequisition(RequisitionViewModel requisition)
        {
            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0 && requisition.State == RequisitionStates.Approved)
            {
                var linesToValidate = requisition.Lines
                    .Where(x => x.QuantityRequired != null && x.QuantityRequired.Value > 0)
                    .ToList();

                if (linesToValidate.Count() > 0)
                {
                    requisition.State = RequisitionStates.Validated;
                    requisition.ResponsibleValidation = this.changedByUserName;
                    requisition.ValidationDate = DateTime.Now;

                    linesToValidate.ForEach(item =>
                    {
                        item.QuantityToProvide = item.QuantityRequired;
                        item.UpdateUser = this.changedByUserName;
                    });

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

        public RequisitionViewModel ValidateLocalMarketFor(RequisitionViewModel requisition)
        {            
            if (requisition != null && requisition.Lines != null && requisition.Lines.Count > 0 && requisition.State == RequisitionStates.Approved)
            {
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
                }
                catch
                {
                    throw new Exception("Ocorreu um erro ao agrupar as linhas.");
                }

                if (purchOrders.Count() > 0)
                {
                    purchOrders.ForEach(purchOrder =>
                    {
                        try
                        {
                            var result = CreateNAVPurchaseOrderFor(purchOrder);
                            if (result.CompletedSuccessfully)
                            {
                                //Update Requisition Lines
                                requisitionLines.ForEach(line =>
                                   line.CreatedOrderNo = result.NAVPrePurchOrderId);

                                bool linesUpdated = DBRequestLine.Update(requisitionLines.ParseToDB());
                                if (linesUpdated)
                                {
                                    requisition.eMessages.Add(new TraceInformation(TraceType.Success, "Criada encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                                }
                            }
                        }
                        catch
                        {
                            requisition.eMessages.Add(new TraceInformation(TraceType.Error, "Ocorreu um erro ao criar encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                        }
                    });

                    if (requisition.eMessages.Any(x => x.Type == TraceType.Success))
                    {
                        //Refresh lines - Get from db
                        var updatedLines = DBRequestLine.GetAllByRequisiçãos(requisition.RequisitionNo);
                        if (updatedLines != null)
                        {
                            requisition.Lines = updatedLines.ParseToViewModel();
                        }
                    }

                    if (requisition.eMessages.Any(x => x.Type == TraceType.Error))
                    {
                        requisition.eReasonCode = 2;
                        requisition.eMessage = "Ocorram erros ao validar o mercado local.";
                    }
                    else
                    {
                        requisition.eReasonCode = 1;
                        requisition.eMessage = "Mercado local validado com sucesso.";
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
                requisition.eReasonCode = 3;
                requisition.eMessage = "O estado da requisição e / ou linhas não cumprem os requisitos de validação.";
            }
            return requisition;
        }

        public RequisitionViewModel CreatePurchaseOrderFor(RequisitionViewModel requisition)
        {
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
                    throw new Exception("Ocorreu um erro ao agrupar as linhas.");
                }

                if (purchOrders.Count() > 0)
                {
                    purchOrders.ForEach(purchOrder =>
                    {
                        try
                        {
                            var result = CreateNAVPurchaseOrderFor(purchOrder);
                            if (result.CompletedSuccessfully)
                            {
                                //Update Requisition Lines
                                requisition.Lines.ForEach(line =>
                                {
                                    line.CreatedOrderNo = result.NAVPrePurchOrderId;
                                    line.UpdateUser = this.changedByUserName;
                                });

                                bool linesUpdated = DBRequestLine.Update(requisition.Lines.ParseToDB());
                                if (linesUpdated)
                                {
                                    requisition.eMessages.Add(new TraceInformation(TraceType.Success, "Criada encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                                }
                            }
                        }
                        catch
                        {
                            requisition.eMessages.Add(new TraceInformation(TraceType.Error, "Ocorreu um erro ao criar encomenda para o fornecedor núm. " + purchOrder.SupplierId + "; "));
                        }
                    });

                    if (requisition.eMessages.Any(x => x.Type == TraceType.Success))
                    {
                        //Refresh lines - Get from db
                        var updatedLines = DBRequestLine.GetAllByRequisiçãos(requisition.RequisitionNo);
                        if (updatedLines != null)
                        {
                            requisition.Lines = updatedLines.ParseToViewModel();
                        }
                    }

                    if (requisition.eMessages.Any(x => x.Type == TraceType.Error))
                    {
                        requisition.eReasonCode = 2;
                        requisition.eMessage = "Ocorram erros ao criar encomenda de compra.";
                    }
                    else
                    {
                        requisition.eReasonCode = 1;
                        requisition.eMessage = "Encomenda de compra criada com sucesso.";
                    }
                }
                else
                {
                    requisition.eReasonCode = 3;
                    requisition.eMessage = "Não existem linhas que cumpram os requisitos de validação do mercado local.";
                }
            }
            return requisition;
        }

        public RequisitionViewModel SendPrePurchaseFor(RequisitionViewModel requisition)
        {
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
                        CreateUser = this.changedByUserName,
                        SupplierNo = line.SupplierNo,
                    })
                    .ToList();

                if (prePurchOrderLines.Count() > 0)
                {
                    bool success = false;
                    try
                    {
                        //Update Requisition Lines
                        requisitionLines.ForEach(line =>
                        {
                            line.SendPrePurchase = true;
                            line.UpdateUser = this.changedByUserName;
                        });

                        var createdLines = DBPrePurchOrderLines.CreateAndUpdateReqLines(prePurchOrderLines.ParseToDB(), requisitionLines.ParseToDB());
                        if (createdLines != null)
                        {
                            var updatedLines = DBRequestLine.GetAllByRequisiçãos(requisition.RequisitionNo);
                            if (updatedLines != null)
                            {
                                requisition.Lines = updatedLines.ParseToViewModel();
                            }
                            success = true;
                        }
                    }
                    catch { }

                    if (success)
                    {
                        requisition.eReasonCode = 1;
                        requisition.eMessage = "Pré-Encomenda enviada com sucesso";
                    }
                    else
                    {
                        requisition.eReasonCode = 2;
                        requisition.eMessage = "Ocorreu um erro ao enviar a pré-encomenda.";
                    }
                }
                else
                {
                    requisition.eReasonCode = 2;
                    requisition.eMessage = " Não existem linhas para enviar.";
                }
            }
            else
            {
                requisition.eReasonCode = 2;
                requisition.eMessage = " O estado da requisição e / ou linhas não cumprem os requisitos.";
            }
            return requisition;
        }

        public ErrorHandler CreateMarketConsultFor(RequisitionViewModel requisition)
        {
            throw new NotImplementedException("CreateMarketConsultFor");
        }
        
        public ErrorHandler CreateTransferShipmentFor(RequisitionViewModel requisition)
        {
            throw new NotImplementedException("CreatePurchaseOrderCommitmentFrom");
        }
        
        private CreatePrePurchOrderResult CreateNAVPurchaseOrderFor(PurchOrderDTO purchOrder)
        {
            CreatePrePurchOrderResult result = new CreatePrePurchOrderResult();

            Task<WSPurchaseInvHeader.Create_Result> createPurchaseHeaderTask = NAVPurchaseHeaderService.CreateAsync(purchOrder, configws);
            createPurchaseHeaderTask.Wait();
            if (createPurchaseHeaderTask.IsCompletedSuccessfully)
            {
                result.NAVPrePurchOrderId = createPurchaseHeaderTask.Result.WSPurchInvHeaderInterm.No;

                Task<WSPurchaseInvLine.CreateMultiple_Result> createPurchaseLinesTask = NAVPurchaseLineService.CreateMultipleAsync(purchOrder, configws);
                createPurchaseLinesTask.Wait();
                if (createPurchaseLinesTask.IsCompletedSuccessfully)
                {
                    try
                    {
                        /*
                         *  Swallow errors at this stage as they will be managed in NAV
                         */
                        Task<WSGenericCodeUnit.FxCabimento_Result> createPurchOrderTask = WSGeneric.CreatePurchaseOrderFitting(purchOrder.NAVPrePurchOrderId, configws);
                        createPurchOrderTask.Wait();
                        if (createPurchOrderTask.IsCompletedSuccessfully)
                        {
                            result.CompletedSuccessfully = true;
                        }
                    }
                    catch { }
                }
            }
            return result;
        }
    }
}
