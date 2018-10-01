﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hydra.Such.Data.Database;
using Hydra.Such.Data.ViewModel.Compras;
using Microsoft.EntityFrameworkCore;
using static Hydra.Such.Data.Enumerations;

namespace Hydra.Such.Data.Logic.Request
{
    public static class DBRequest
    {
        #region CRUD
        
        public static List<Requisição> GetAll()
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Requisição.ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static List<Requisição> GetByState(RequisitionStates state)
        {
            try
            {
                List<RequisitionStates> states = new List<RequisitionStates>() { state };
                return GetByState(states);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static List<Requisição> GetByState(List<RequisitionStates> states)
        {
            try
            {
                List<int> stateValues = states.Cast<int>().ToList();

                using (var ctx = new SuchDBContext())
                {
                    return ctx.Requisição
                        .Include("LinhasRequisição")
                        //AROMAO 01/10/2018
                        //.Include(x => x.RequisicoesRegAlteracoes)
                        .Where(x => stateValues.Contains(x.Estado.Value))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static Requisição GetById(string requestId)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Requisição
                        .Include(x => x.LinhasRequisição)//("LinhasRequisição")
                        //AROMAO 01/10/2018
                        //.Include(x => x.RequisicoesRegAlteracoes)
                        .SingleOrDefault(x => x.NºRequisição == requestId);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static Requisição Create(Requisição ObjectToCreate)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    ObjectToCreate.DataHoraCriação = DateTime.Now;
                    ctx.Requisição.Add(ObjectToCreate);
                    ctx.SaveChanges();
                }

                return ObjectToCreate;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public static Requisição Update(Requisição objectToUpdate, bool updateLines = false, bool addLogEntry = false)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    if (updateLines && objectToUpdate.LinhasRequisição != null)
                        DBRequestLine.Update(objectToUpdate.LinhasRequisição.ToList(), ctx);
                    if (addLogEntry)
                    {
                        var logEntry = new RequisicoesRegAlteracoes();
                        logEntry.ModificadoEm = DateTime.Now;
                        logEntry.ModificadoPor = objectToUpdate.UtilizadorModificação;
                        logEntry.NºRequisição = objectToUpdate.NºRequisição;
                        if (objectToUpdate.Estado.HasValue)
                            logEntry.Estado = objectToUpdate.Estado.Value;

                        ctx.RequisicoesRegAlteracoes.Add(logEntry);
                    }
                    objectToUpdate.DataHoraModificação = DateTime.Now;
                    ctx.Requisição.Update(objectToUpdate);
                    ctx.SaveChanges();

                    objectToUpdate = GetById(objectToUpdate.NºRequisição);
                }

                return objectToUpdate;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static Requisição UpdateHeaderAndLines(Requisição item, bool addLogEntry)
        {
            return Update(item, true, addLogEntry);
        }

        public static bool Delete(Requisição ObjectToDelete)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    //if (ObjectToDelete.LinhasRequisição.Count() > 0)
                    //    ctx.LinhasRequisição.RemoveRange(ObjectToDelete.LinhasRequisição);
                    //if (ObjectToDelete.RequisicoesRegAlteracoes.Count() > 0)
                    //    ctx.RequisicoesRegAlteracoes.RemoveRange(ObjectToDelete.RequisicoesRegAlteracoes);
                    ctx.Requisição.Remove(ObjectToDelete);
                    ctx.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public static List<Requisição> GetByProcedimento(string procedimentoNo)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Requisição.Where(x => x.NºProcedimentoCcp == procedimentoNo).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

        public static List<Requisição> GetReqByUserAreaStatus(string userName, RequisitionStates status)
        {
            return GetReqByUserAreaStatus(userName, new List<RequisitionStates> { status });
        }

        public static List<Requisição> GetReqByUserAreaStatus(string UserName, List<RequisitionStates> status)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    var statusValues = status.Cast<int>().ToList();
                    
                    return ctx.Requisição.Where(x => x.UtilizadorCriação == UserName && statusValues.Contains(x.Estado.Value) && !x.ModeloDeRequisição.HasValue || !x.ModeloDeRequisição.Value).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static List<Requisição> GetReqByUser(string UserName)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Requisição.Where(x => x.UtilizadorCriação == UserName && !x.ModeloDeRequisição.HasValue || !x.ModeloDeRequisição.Value).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #region Parse Utilities
        public static RequisiçãoHist TransferToRequisitionHist(this RequisitionViewModel item)
        {
            if (item != null)
            {
                return new RequisiçãoHist()
                {
                    NºRequisição = item.RequisitionNo,
                    Área = item.Area,
                    Estado = item.State.HasValue ? (int)item.State.Value : (int?)null,
                    NºProjeto = item.ProjectNo,
                    CódigoRegião = item.RegionCode,
                    CódigoÁreaFuncional = item.FunctionalAreaCode,
                    CódigoCentroResponsabilidade = item.CenterResponsibilityCode,
                    CódigoLocalização = item.LocalCode,
                    NºFuncionário = item.EmployeeNo,
                    Viatura = item.Vehicle,
                    DataReceção = !string.IsNullOrEmpty(item.ReceivedDate) ? Convert.ToDateTime(item.ReceivedDate) : (DateTime?)null,
                    Urgente = item.Urgent,
                    Amostra = item.Sample,
                    Anexo = item.Attachment,
                    Imobilizado = item.Immobilized,
                    CompraADinheiro = item.BuyCash,
                    CódigoLocalRecolha = item.LocalCollectionCode,
                    CódigoLocalEntrega = item.LocalDeliveryCode,
                    Observações = item.Comments,
                    ModeloDeRequisição = item.RequestModel,
                    DataHoraCriação = !string.IsNullOrEmpty(item.CreateDate) ? Convert.ToDateTime(item.CreateDate) : (DateTime?)null,
                    UtilizadorCriação = item.CreateUser,
                    DataHoraModificação = item.UpdateDate,
                    UtilizadorModificação = item.UpdateUser,
                    CabimentoOrçamental = item.RelatedSearches,
                    Exclusivo = item.Exclusive,
                    JáExecutado = item.AlreadyPerformed,
                    Equipamento = item.Equipment,
                    ReposiçãoDeStock = item.StockReplacement,
                    Reclamação = item.Reclamation,
                    NºRequisiçãoReclamada = item.RequestReclaimNo,
                    ResponsávelCriação = item.ResponsibleCreation,
                    ResponsávelAprovação = item.ResponsibleApproval,
                    ResponsávelValidação = item.ResponsibleValidation,
                    ResponsávelReceção = item.ResponsibleReception,
                    DataAprovação = item.ApprovalDate,
                    DataValidação = item.ValidationDate,
                    UnidadeProdutivaAlimentação = item.UnitFoodProduction,
                    RequisiçãoNutrição = item.RequestNutrition,
                    RequisiçãoDetergentes = item.RequestforDetergents,
                    NºProcedimentoCcp = item.ProcedureCcpNo,
                    Aprovadores = item.Approvers,
                    MercadoLocal = item.LocalMarket,
                    RegiãoMercadoLocal = item.LocalMarketRegion,
                    ReparaçãoComGarantia = item.RepairWithWarranty,
                    Emm = item.Emm,
                    DataEntregaArmazém = !string.IsNullOrEmpty(item.WarehouseDeliveryDate) ? Convert.ToDateTime(item.WarehouseDeliveryDate) : (DateTime?)null,
                    LocalDeRecolha = item.LocalCollection,
                    MoradaRecolha = item.CollectionAddress,
                    Morada2Recolha = item.Collection2Address,
                    CódigoPostalRecolha = item.CollectionPostalCode,
                    LocalidadeRecolha = item.CollectionLocality,
                    ContatoRecolha = item.CollectionContact,
                    ResponsávelReceçãoRecolha = item.CollectionResponsibleReception,
                    LocalEntrega = item.LocalDelivery,
                    MoradaEntrega = item.DeliveryAddress,
                    Morada2Entrega = item.Delivery2Address,
                    CódigoPostalEntrega = item.DeliveryPostalCode,
                    LocalidadeEntrega = item.LocalityDelivery,
                    ContatoEntrega = item.DeliveryContact,
                    ResponsávelReceçãoReceção = item.ResponsibleReceptionReception,
                    NºFatura = item.InvoiceNo,
                    DataMercadoLocal = item.LocalMarketDate,
                    DataRequisição = !string.IsNullOrEmpty(item.RequisitionDate) ? Convert.ToDateTime(item.RequisitionDate) : (DateTime?)null,
                    NºConsultaMercado = item.MarketInquiryNo,
                    NºEncomenda = item.OrderNo,
                    Orçamento = item.Budget,
                    ValorEstimado = item.EstimatedValue,
                    PrecoIvaincluido = item.PricesIncludingVAT,
                    Adiantamento = item.InAdvance,
                    PedirOrcamento = item.PedirOrcamento
                };
            }
            return null;
        }

        public static List<LinhasRequisiçãoHist> TransferToRequisitionLinesHist(this List<RequisitionLineViewModel> Linhas)
        {
            List<LinhasRequisiçãoHist> LinhasREQHist = new List<LinhasRequisiçãoHist>();
            if (Linhas.Count > 0)
            {
                Linhas.ForEach(Linha =>
                {
                    LinhasRequisiçãoHist LinhaHist = new LinhasRequisiçãoHist()
                    {
                        NºRequisição = Linha.RequestNo,
                        NºLinha = (int)Linha.LineNo,
                        Tipo = Linha.Type,
                        Código = Linha.Code,
                        Descrição = Linha.Description,
                        Descrição2 = Linha.Description2,
                        CódigoUnidadeMedida = Linha.UnitMeasureCode,
                        CódigoLocalização = Linha.LocalCode,
                        MercadoLocal = Linha.LocalMarket,
                        QuantidadeARequerer = Linha.QuantityToRequire,
                        QuantidadeRequerida = Linha.QuantityRequired,
                        QuantidadeDisponibilizada = Linha.QuantityToProvide,
                        QuantidadeAReceber = Linha.QuantityAvailable,
                        QuantidadeRecebida = Linha.QuantityReceivable,
                        QuantidadePendente = Linha.QuantityPending,
                        CustoUnitário = Linha.UnitCost,
                        DataReceçãoEsperada = !string.IsNullOrEmpty(Linha.ExpectedReceivingDate) ? Convert.ToDateTime(Linha.ExpectedReceivingDate) : (DateTime?)null,
                        Faturável = Linha.Billable,
                        NºProjeto = Linha.ProjectNo,
                        CódigoRegião = Linha.RegionCode,
                        CódigoÁreaFuncional = Linha.FunctionalAreaCode,
                        CódigoCentroResponsabilidade = Linha.CenterResponsibilityCode,
                        NºFuncionário = Linha.FunctionalNo,
                        Viatura = Linha.Vehicle,
                        DataHoraCriação = Linha.CreateDateTime,
                        UtilizadorCriação = Linha.CreateUser,
                        DataHoraModificação = Linha.UpdateDateTime,
                        UtilizadorModificação = Linha.UpdateUser,
                        QtdPorUnidadeDeMedida = Linha.QtyByUnitOfMeasure,
                        PreçoUnitárioVenda = Linha.UnitCostsould,
                        ValorOrçamento = Linha.BudgetValue,
                        NºLinhaOrdemManutenção = Linha.MaintenanceOrderLineNo,
                        CriarConsultaMercado = Linha.CreateMarketSearch,
                        EnviarPréCompra = Linha.SubmitPrePurchase,
                        EnviadoPréCompra = Linha.SendPrePurchase,
                        DataMercadoLocal = !string.IsNullOrEmpty(Linha.LocalMarketDate) ? Convert.ToDateTime(Linha.LocalMarketDate) : (DateTime?)null,
                        UserMercadoLocal = Linha.LocalMarketUser,
                        EnviadoParaCompras = Linha.SendForPurchase,
                        DataEnvioParaCompras = !string.IsNullOrEmpty(Linha.SendForPurchaseDate) ? Convert.ToDateTime(Linha.SendForPurchaseDate) : (DateTime?)null,
                        ValidadoCompras = Linha.PurchaseValidated,
                        RecusadoCompras = Linha.PurchaseRefused,
                        MotivoRecusaMercLocal = Linha.ReasonToRejectionLocalMarket,
                        DataRecusaMercLocal = !string.IsNullOrEmpty(Linha.RejectionLocalMarketDate) ? Convert.ToDateTime(Linha.RejectionLocalMarketDate) : (DateTime?)null,
                        IdCompra = Linha.PurchaseId,
                        NºFornecedor = Linha.SupplierNo,
                        NºEncomendaAberto = Linha.OpenOrderNo,
                        NºLinhaEncomendaAberto = Linha.OpenOrderLineNo,
                        NºDeConsultaMercadoCriada = Linha.QueryCreatedMarketNo,
                        NºEncomendaCriada = Linha.CreatedOrderNo,
                        CódigoProdutoFornecedor = Linha.SupplierProductCode,
                        UnidadeProdutivaNutrição = Linha.UnitNutritionProduction,
                        RegiãoMercadoLocal = Linha.MarketLocalRegion,
                        NºCliente = Linha.CustomerNo,
                        Aprovadores = Linha.Approvers,
                        Urgente = Linha.Urgent,
                        GrupoRegistoIvanegocio = Linha.VATBusinessPostingGroup,
                        GrupoRegistoIvaproduto = Linha.VATProductPostingGroup,
                        PercentagemDesconto = Linha.DiscountPercentage
                    };

                    LinhasREQHist.Add(LinhaHist);
                });
            }
            return LinhasREQHist;
        }

        public static RequisitionViewModel ParseToViewModel(this Requisição item)
        {
            if (item != null)
            {
                return new RequisitionViewModel()
                {
                    RequisitionNo = item.NºRequisição,
                    Area = item.Área,
                    State = item.Estado.HasValue && Enum.IsDefined(typeof(RequisitionStates), item.Estado.Value) ? (RequisitionStates)item.Estado.Value : (RequisitionStates?)null,
                    ProjectNo = item.NºProjeto,
                    RegionCode = item.CódigoRegião,
                    FunctionalAreaCode = item.CódigoÁreaFuncional,
                    CenterResponsibilityCode = item.CódigoCentroResponsabilidade,
                    LocalCode = item.CódigoLocalização,
                    EmployeeNo = item.NºFuncionário,
                    Vehicle = item.Viatura,
                    ReceivedDate = !item.DataReceção.HasValue ? "" : item.DataReceção.Value.ToString("yyyy-MM-dd"),
                    Urgent = item.Urgente,
                    Sample = item.Amostra,
                    Attachment = item.Anexo,
                    Immobilized = item.Imobilizado,
                    BuyCash = item.CompraADinheiro,
                    LocalCollectionCode = item.CódigoLocalRecolha,
                    LocalDeliveryCode = item.CódigoLocalEntrega,
                    Comments = item.Observações,
                    RequestModel = item.ModeloDeRequisição,
                    CreateDate = !item.DataHoraCriação.HasValue ? "" : item.DataHoraCriação.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateUser = item.UtilizadorCriação,
                    UpdateDate = item.DataHoraModificação,
                    UpdateUser = item.UtilizadorModificação,
                    RelatedSearches = item.CabimentoOrçamental,
                    Exclusive = item.Exclusivo,
                    AlreadyPerformed = item.JáExecutado,
                    Equipment = item.Equipamento,
                    StockReplacement = item.ReposiçãoDeStock,
                    Reclamation = item.Reclamação,
                    RequestReclaimNo = item.NºRequisiçãoReclamada,
                    ResponsibleCreation = item.ResponsávelCriação,
                    ResponsibleApproval = item.ResponsávelAprovação,
                    ResponsibleValidation = item.ResponsávelValidação,
                    ResponsibleReception = item.ResponsávelReceção,
                    ApprovalDate = item.DataAprovação,
                    ValidationDate = item.DataValidação,
                    UnitFoodProduction = item.UnidadeProdutivaAlimentação,
                    RequestNutrition = item.RequisiçãoNutrição,
                    RequestforDetergents = item.RequisiçãoDetergentes,
                    ProcedureCcpNo = item.NºProcedimentoCcp,
                    Approvers = item.Aprovadores,
                    LocalMarket = item.MercadoLocal,
                    LocalMarketRegion = item.RegiãoMercadoLocal,
                    RepairWithWarranty = item.ReparaçãoComGarantia,
                    Emm = item.Emm,
                    WarehouseDeliveryDate = !item.DataEntregaArmazém.HasValue ? "" : item.DataEntregaArmazém.Value.ToString("yyyy-MM-dd"),
                    LocalCollection = item.LocalDeRecolha,
                    CollectionAddress = item.MoradaRecolha,
                    Collection2Address = item.Morada2Recolha,
                    CollectionPostalCode = item.CódigoPostalRecolha,
                    CollectionLocality = item.LocalidadeRecolha,
                    CollectionContact = item.ContatoRecolha,
                    CollectionResponsibleReception = item.ResponsávelReceçãoRecolha,
                    LocalDelivery = item.LocalEntrega,
                    DeliveryAddress = item.MoradaEntrega,
                    Delivery2Address = item.Morada2Entrega,
                    DeliveryPostalCode = item.CódigoPostalEntrega,
                    LocalityDelivery = item.LocalidadeEntrega,
                    DeliveryContact = item.ContatoEntrega,
                    ResponsibleReceptionReception = item.ResponsávelReceçãoReceção,
                    InvoiceNo = item.NºFatura,
                    LocalMarketDate = item.DataMercadoLocal,
                    RequisitionDate = !item.DataRequisição.HasValue ? "" : item.DataRequisição.Value.ToString("yyyy-MM-dd"),
                    MarketInquiryNo = item.NºConsultaMercado,
                    OrderNo = item.NºEncomenda,
                    Budget = item.Orçamento,
                    EstimatedValue = item.ValorEstimado,
                    PricesIncludingVAT = item.PrecoIvaincluido.HasValue ? item.PrecoIvaincluido.Value : false,
                    InAdvance = item.Adiantamento.HasValue ? item.Adiantamento.Value : false,
                    PedirOrcamento = item.PedirOrcamento,

                    Lines = item.LinhasRequisição.ToList().ParseToViewModel(),
                    //AROMAO 01/10/2018
                    //ChangeLog = item.RequisicoesRegAlteracoes.ToList().ParseToViewModel()
                };
            }
            return null;
        }

        public static List<RequisitionViewModel> ParseToViewModel(this List<Requisição> items)
        {
            List<RequisitionViewModel> parsedItems = new List<RequisitionViewModel>();
            if (items != null)
                items.ForEach(x =>
                    parsedItems.Add(x.ParseToViewModel()));
            return parsedItems;
        }

        public static Requisição ParseToDB(this RequisitionViewModel item)
        {
            if (item != null)
            {
                return new Requisição()
                {
                    NºRequisição = item.RequisitionNo,
                    Área = item.Area,
                    Estado = item.State.HasValue ? (int)item.State.Value : (int?)null,
                    NºProjeto = item.ProjectNo,
                    CódigoRegião = item.RegionCode,
                    CódigoÁreaFuncional = item.FunctionalAreaCode,
                    CódigoCentroResponsabilidade = item.CenterResponsibilityCode,
                    CódigoLocalização = item.LocalCode,
                    NºFuncionário = item.EmployeeNo,
                    Viatura = item.Vehicle,
                    DataReceção = !string.IsNullOrEmpty(item.ReceivedDate) ? Convert.ToDateTime(item.ReceivedDate) : (DateTime?)null,
                    Urgente = item.Urgent,
                    Amostra = item.Sample,
                    Anexo = item.Attachment,
                    Imobilizado = item.Immobilized,
                    CompraADinheiro = item.BuyCash,
                    CódigoLocalRecolha = item.LocalCollectionCode,
                    CódigoLocalEntrega = item.LocalDeliveryCode,
                    Observações = item.Comments,
                    ModeloDeRequisição = item.RequestModel,
                    DataHoraCriação = !string.IsNullOrEmpty(item.CreateDate) ? Convert.ToDateTime(item.CreateDate) : (DateTime?)null,
                    UtilizadorCriação = item.CreateUser,
                    DataHoraModificação = item.UpdateDate,
                    UtilizadorModificação = item.UpdateUser,
                    CabimentoOrçamental = item.RelatedSearches,
                    Exclusivo = item.Exclusive,
                    JáExecutado = item.AlreadyPerformed,
                    Equipamento = item.Equipment,
                    ReposiçãoDeStock = item.StockReplacement,
                    Reclamação = item.Reclamation,
                    NºRequisiçãoReclamada = item.RequestReclaimNo,
                    ResponsávelCriação = item.ResponsibleCreation,
                    ResponsávelAprovação = item.ResponsibleApproval,
                    ResponsávelValidação = item.ResponsibleValidation,
                    ResponsávelReceção = item.ResponsibleReception,
                    DataAprovação = item.ApprovalDate,
                    DataValidação = item.ValidationDate,
                    UnidadeProdutivaAlimentação = item.UnitFoodProduction,
                    RequisiçãoNutrição = item.RequestNutrition,
                    RequisiçãoDetergentes = item.RequestforDetergents,
                    NºProcedimentoCcp = item.ProcedureCcpNo,
                    Aprovadores = item.Approvers,
                    MercadoLocal = item.LocalMarket,
                    RegiãoMercadoLocal = item.LocalMarketRegion,
                    ReparaçãoComGarantia = item.RepairWithWarranty,
                    Emm = item.Emm,
                    DataEntregaArmazém = !string.IsNullOrEmpty(item.WarehouseDeliveryDate) ? Convert.ToDateTime(item.WarehouseDeliveryDate) : (DateTime?)null,
                    LocalDeRecolha = item.LocalCollection,
                    MoradaRecolha = item.CollectionAddress,
                    Morada2Recolha = item.Collection2Address,
                    CódigoPostalRecolha = item.CollectionPostalCode,
                    LocalidadeRecolha = item.CollectionLocality,
                    ContatoRecolha = item.CollectionContact,
                    ResponsávelReceçãoRecolha = item.CollectionResponsibleReception,
                    LocalEntrega = item.LocalDelivery,
                    MoradaEntrega = item.DeliveryAddress,
                    Morada2Entrega = item.Delivery2Address,
                    CódigoPostalEntrega = item.DeliveryPostalCode,
                    LocalidadeEntrega = item.LocalityDelivery,
                    ContatoEntrega = item.DeliveryContact,
                    ResponsávelReceçãoReceção = item.ResponsibleReceptionReception,
                    NºFatura = item.InvoiceNo,
                    DataMercadoLocal = item.LocalMarketDate,
                    DataRequisição = !string.IsNullOrEmpty(item.RequisitionDate) ? Convert.ToDateTime(item.RequisitionDate) : (DateTime?)null,
                    NºConsultaMercado = item.MarketInquiryNo,
                    NºEncomenda = item.OrderNo,
                    Orçamento = item.Budget,
                    ValorEstimado = item.EstimatedValue,
                    PrecoIvaincluido = item.PricesIncludingVAT,
                    Adiantamento = item.InAdvance,
                    PedirOrcamento = item.PedirOrcamento,

                    LinhasRequisição = item.Lines.ParseToDB()
                    //AROMAO 01/10/2018
                    //RequisicoesRegAlteracoes = item.ChangeLog.ParseToDB()
                };
            }
            return null;
        }

        public static List<Requisição> ParseToDB(this List<RequisitionViewModel> items)
        {
            List<Requisição> parsedItems = new List<Requisição>();
            if (items != null)
                items.ForEach(x =>
                    parsedItems.Add(x.ParseToDB()));
            return parsedItems;
        }

        
        public static RequisitionChangeLog ParseToViewModel(this RequisicoesRegAlteracoes item)
        {
            if (item != null)
            {
                return new RequisitionChangeLog()
                {
                    Id = item.Id,
                    RequisitionNo = item.NºRequisição,
                    State = (RequisitionStates)item.Estado,
                    StateDescription = EnumHelper.GetDescriptionFor(typeof(RequisitionStates), item.Estado),
                    ModifiedAt = item.ModificadoEm,
                    ModifiedAtAsString = item.ModificadoEm.ToString("yyyy-MM-dd HH:mm:ss"),
                    ModifiedBy = item.ModificadoPor
                };
            }
            return null;
        }

        public static List<RequisitionChangeLog> ParseToViewModel(this List<RequisicoesRegAlteracoes> items)
        {
            List<RequisitionChangeLog> parsedItems = new List<RequisitionChangeLog>();
            if (items != null)
                items.ForEach(x =>
                    parsedItems.Add(x.ParseToViewModel()));
            return parsedItems;
        }

        public static RequisicoesRegAlteracoes ParseToDB(this RequisitionChangeLog item)
        {
            if (item != null)
            {
                return new RequisicoesRegAlteracoes()
                {
                    Id = item.Id,
                    NºRequisição = item.RequisitionNo,
                    Estado = (int)item.State,
                    ModificadoEm = item.ModifiedAt,
                    ModificadoPor = item.ModifiedBy
                };
            }
            return null;
        }

        public static List<RequisicoesRegAlteracoes> ParseToDB(this List<RequisitionChangeLog> items)
        {
            List<RequisicoesRegAlteracoes> parsedItems = new List<RequisicoesRegAlteracoes>();
            if (items != null)
                items.ForEach(x =>
                    parsedItems.Add(x.ParseToDB()));
            return parsedItems;
        }
        #endregion
    }
}
