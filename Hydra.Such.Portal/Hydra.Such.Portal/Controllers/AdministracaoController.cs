﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Office.Interop.Excel;
using Hydra.Such.Data.ViewModel;
using Hydra.Such.Data.Logic;
using Hydra.Such.Data.Database;
using Hydra.Such.Data.Logic.Project;
using Hydra.Such.Data.Logic.ProjectDiary;
using Hydra.Such.Data.ViewModel.ProjectDiary;
using Hydra.Such.Data.ViewModel.ProjectView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Hydra.Such.Data.ViewModel.Viaturas;
using Hydra.Such.Data.Logic.Viatura;
using Hydra.Such.Data.ViewModel.FH;
using Hydra.Such.Data.Logic.FolhaDeHora;
using Hydra.Such.Portal.Configurations;
using Hydra.Such.Data.NAV;
using Hydra.Such.Data.ViewModel.Compras;
using Hydra.Such.Data.Logic.Compras;
using Hydra.Such.Data.Logic.Approvals;
using Hydra.Such.Data.ViewModel.Approvals;
using Microsoft.Extensions.Options;
using Hydra.Such.Data;
using System.IO;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http;
using System.Drawing;
using System.Globalization;

namespace Hydra.Such.Portal.Controllers
{
    [Authorize]
    public class AdministracaoController : Controller
    {
        private readonly NAVConfigurations _config;
        private readonly NAVWSConfigurations _configws;
        private readonly GeneralConfigurations _generalConfig;

        public AdministracaoController(IOptions<NAVConfigurations> appSettings, IOptions<NAVWSConfigurations> NAVWSConfigs, IOptions<GeneralConfigurations> appSettingsGeneral)
        {
            _config = appSettings.Value;
            _configws = NAVWSConfigs.Value;
            _generalConfig = appSettingsGeneral.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region Utilizadores
        public IActionResult ConfiguracaoUtilizadores()
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetListUsers()
        {
            List<ConfigUtilizadores> result = DBUserConfigurations.GetAll();

            if (result != null)
            {
                result.ForEach(Utilizador =>
                {
                    var nomeRegião = DBNAV2017DimensionValues.GetById(_config.NAVDatabaseName, _config.NAVCompanyName, 1, User.Identity.Name, Utilizador.RegiãoPorDefeito).FirstOrDefault();
                    Utilizador.RegiãoPorDefeito = nomeRegião == null ? "" : nomeRegião.Name;

                    var nomeÁreaPorDefeito = DBNAV2017DimensionValues.GetById(_config.NAVDatabaseName, _config.NAVCompanyName, 2, User.Identity.Name, Utilizador.AreaPorDefeito).FirstOrDefault();
                    Utilizador.AreaPorDefeito = nomeÁreaPorDefeito == null ? "" : nomeÁreaPorDefeito.Name;

                    var nomeCentroRespPorDefeito = DBNAV2017DimensionValues.GetById(_config.NAVDatabaseName, _config.NAVCompanyName, 3, User.Identity.Name, Utilizador.CentroRespPorDefeito).FirstOrDefault();
                    Utilizador.CentroRespPorDefeito = nomeCentroRespPorDefeito == null ? "" : nomeCentroRespPorDefeito.Name;
                });
            };

            return Json(result);
        }


        public IActionResult ConfiguracaoUtilizadoresDetalhes(string id)
        {
            ViewBag.UserId = id;
            return View();
        }

        [HttpPost]
        public JsonResult GetUserConfigData([FromBody] UserConfigurationsViewModel data)
        {
            ConfigUtilizadores CU = DBUserConfigurations.GetById(data.IdUser);
            UserConfigurationsViewModel result = new UserConfigurationsViewModel()
            {
                IdUser = "",
                UserAccesses = new List<UserAccessesViewModel>(),
                UserProfiles = new List<ProfileModelsViewModel>()
            };

            if (CU != null)
            {
                result.IdUser = CU.IdUtilizador;
                result.Name = CU.Nome;
                result.Active = CU.Ativo;
                result.Administrator = CU.Administrador;
                result.Regiao = CU.RegiãoPorDefeito;
                result.Area = CU.AreaPorDefeito;
                result.Cresp = CU.CentroRespPorDefeito;

                result.UserAccesses = DBUserAccesses.GetByUserId(data.IdUser).Select(x => new UserAccessesViewModel()
                {
                    IdUser = x.IdUtilizador,
                    Area = x.Área,
                    Feature = x.Funcionalidade,
                    Create = x.Inserção,
                    Read = x.Leitura,
                    Update = x.Modificação,
                    Delete = x.Eliminação
                }).ToList();

                result.UserProfiles = DBProfileModels.GetByUserId(data.IdUser).Select(x => new ProfileModelsViewModel()
                {
                    Id = x.Id,
                    Description = x.Descrição
                }).ToList();

                result.AllowedUserDimensions = DBUserDimensions.GetByUserId(data.IdUser).ParseToViewModel();

                result.UserAcessosLocalizacoes = DBAcessosLocalizacoes.GetByUserId(data.IdUser).ParseToViewModel();
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateUserConfig([FromBody] UserConfigurationsViewModel data)
        {
            ConfigUtilizadores ObjectCreated = DBUserConfigurations.Create(new ConfigUtilizadores()
            {
                IdUtilizador = data.IdUser,
                Nome = data.Name,
                Administrador = data.Administrator,
                Ativo = data.Active,
                RegiãoPorDefeito = data.Regiao,
                AreaPorDefeito = data.Area,
                CentroRespPorDefeito = data.Cresp,
                UtilizadorCriação = User.Identity.Name,
            });

            data.IdUser = ObjectCreated.IdUtilizador;

            //Add Accesses
            data.UserAccesses.ForEach(x =>
            {
                DBUserAccesses.Create(new AcessosUtilizador()
                {
                    IdUtilizador = ObjectCreated.IdUtilizador,
                    Área = x.Area,
                    Funcionalidade = x.Feature,
                    Inserção = x.Create,
                    Leitura = x.Read,
                    Modificação = x.Update,
                    Eliminação = x.Delete,
                    UtilizadorCriação = User.Identity.Name
                });
            });

            //Add Profiles
            data.UserProfiles.ForEach(x =>
            {
                DBUserProfiles.Create(new PerfisUtilizador()
                {
                    IdUtilizador = ObjectCreated.IdUtilizador,
                    IdPerfil = x.Id,
                    UtilizadorCriação = User.Identity.Name
                });
            });

            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateUserConfig([FromBody] UserConfigurationsViewModel data)
        {
            //Update UserConfig
            ConfigUtilizadores userConfig = DBUserConfigurations.GetById(data.IdUser);
            if (userConfig == null)
            {
                data.eReasonCode = 1;
                data.eMessage = "Não foi possivel obter o utilizador.";
            }
            else
            {
                userConfig.IdUtilizador = data.IdUser;
                userConfig.Nome = data.Name;
                userConfig.Ativo = data.Active;
                userConfig.Administrador = data.Administrator;
                userConfig.RegiãoPorDefeito = data.Regiao;
                userConfig.AreaPorDefeito = data.Area;
                userConfig.CentroRespPorDefeito = data.Cresp;
                userConfig.DataHoraModificação = DateTime.Now;
                userConfig.UtilizadorModificação = User.Identity.Name;
                DBUserConfigurations.Update(userConfig);

                #region Update Accesses

                //Get Existing from db
                var userAccesses = DBUserAccesses.GetByUserId(data.IdUser);

                //Get items to delete (for changed keys delete old, create new)
                var userAccessesToDelete = userAccesses
                    .Where(x => !data.UserAccesses
                        .Any(y => y.Area == x.Área &&
                            y.Feature == x.Funcionalidade))
                    .ToList();
                //Delete 
                if (userAccessesToDelete.Count > 0)
                {
                    bool uaSuccessfullyDeleted = DBUserAccesses.Delete(userAccessesToDelete);
                    if (!uaSuccessfullyDeleted)
                    {
                        data.eMessage = "Ocorreu um erro ao eliminar os acessos do utilizador.";
                    }
                }

                //Create (for changed keys) or Update existing
                data.UserAccesses.ForEach(userAccess =>
                    {
                        var updatedUA = userAccesses.SingleOrDefault(x => x.Área == userAccess.Area &&
                            x.Funcionalidade == userAccess.Feature);

                        if (updatedUA == null)
                        {
                            //Create
                            updatedUA = new AcessosUtilizador()
                            {
                                IdUtilizador = data.IdUser,
                                Área = userAccess.Area,
                                Funcionalidade = userAccess.Feature,
                                UtilizadorCriação = User.Identity.Name,
                                DataHoraCriação = DateTime.Now
                            };
                            updatedUA = DBUserAccesses.Create(updatedUA);
                        }
                        //Update
                        updatedUA.Eliminação = userAccess.Delete.HasValue ? userAccess.Delete.Value : false;
                        updatedUA.Inserção = userAccess.Create.HasValue ? userAccess.Create.Value : false;
                        updatedUA.Leitura = userAccess.Read.HasValue ? userAccess.Read.Value : false;
                        updatedUA.Modificação = userAccess.Update.HasValue ? userAccess.Update.Value : false;

                        updatedUA.UtilizadorModificação = User.Identity.Name;
                        updatedUA.DataHoraModificação = DateTime.Now;

                        DBUserAccesses.Update(updatedUA);
                    }
                );
                #endregion

                #region Update Profiles

                //Get Existing from db
                var userProfiles = DBUserProfiles.GetByUserId(data.IdUser);

                //Get items to delete (for changed keys delete old, create new)
                var userProfilesToDelete = userProfiles
                    .Where(x => !data.UserProfiles
                        .Any(y => y.Id == x.IdPerfil))
                    .ToList();

                //Delete 
                if (userProfilesToDelete.Count > 0)
                {
                    bool upSuccessfullyDeleted = DBUserProfiles.Delete(userProfilesToDelete);
                    if (!upSuccessfullyDeleted)
                    {
                        data.eMessage = "Ocorreu um erro ao eliminar os perfis do utilizador.";
                    }
                }

                //Create (for changed keys) or Update existing
                data.UserProfiles.ForEach(userProfile =>
                {
                    var updatedUP = userProfiles.SingleOrDefault(x => x.IdPerfil == userProfile.Id);

                    if (updatedUP == null)
                    {
                        //Create
                        updatedUP = new PerfisUtilizador()
                        {
                            IdUtilizador = data.IdUser,
                            IdPerfil = userProfile.Id,
                            UtilizadorCriação = User.Identity.Name,
                            DataHoraCriação = DateTime.Now
                        };
                        updatedUP = DBUserProfiles.Create(updatedUP);
                    }
                    //Update
                    updatedUP.UtilizadorModificação = User.Identity.Name;
                    updatedUP.DataHoraModificação = DateTime.Now;

                    DBUserProfiles.Update(updatedUP);
                });

                #endregion

                #region Update AllowedUserDimemsions

                //Get Existing from db
                var userDimensions = DBUserDimensions.GetByUserId(data.IdUser);

                //Get items to delete (for changed keys delete old, create new)
                var userDimensionsToDelete = userDimensions
                    .Where(x => !data.AllowedUserDimensions
                        .Any(y => y.Dimension == x.Dimensão &&
                            y.DimensionValue == x.ValorDimensão))
                    .ToList();

                //Delete 
                if (userDimensionsToDelete.Count > 0)
                {
                    bool udSuccessfullyDeleted = DBUserDimensions.Delete(userDimensionsToDelete);
                    if (!udSuccessfullyDeleted)
                    {
                        data.eMessage = "Ocorreu um erro ao eliminar as dimensões permitidas ao utilizador.";
                    }
                }

                //Create (for changed keys) or Update existing
                data.AllowedUserDimensions.ForEach(userDimension =>
                {
                    var updatedUD = userDimensions.SingleOrDefault(x => x.Dimensão == userDimension.Dimension &&
                        x.ValorDimensão == userDimension.DimensionValue);

                    if (updatedUD == null)
                    {
                        //Create
                        updatedUD = new AcessosDimensões()
                        {
                            IdUtilizador = data.IdUser,
                            Dimensão = userDimension.Dimension,
                            ValorDimensão = userDimension.DimensionValue,
                            UtilizadorCriação = User.Identity.Name,
                            DataHoraCriação = DateTime.Now
                        };
                        updatedUD = DBUserDimensions.Create(updatedUD);
                    }
                    //Update
                    updatedUD.UtilizadorModificação = User.Identity.Name;
                    updatedUD.DataHoraModificação = DateTime.Now;

                    DBUserDimensions.Update(updatedUD);
                });

                #endregion
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteUserConfig([FromBody] UserConfigurationsViewModel data)
        {
            ConfigUtilizadores UCObj = DBUserConfigurations.GetById(data.IdUser);

            //Remover os acessos os acessos
            DBUserAccesses.DeleteAllFromUser(data.IdUser);

            //Remover os acessos às dimensões
            DBUserDimensions.DeleteAllFromUser(data.IdUser);

            UCObj.Ativo = false;

            DBUserConfigurations.Update(UCObj);
            return Json(data);
        }

        [HttpPost]
        public JsonResult CreateUserDimension([FromBody] UserDimensionsViewModel data)
        {
            bool result = false;
            try
            {
                AcessosDimensões userDimension = new AcessosDimensões();
                userDimension.UtilizadorCriação = User.Identity.Name;
                userDimension.DataHoraCriação = DateTime.Now;
                userDimension.IdUtilizador = data.UserId;
                userDimension.Dimensão = data.Dimension;
                userDimension.ValorDimensão = data.DimensionValue;

                var dbCreateResult = DBUserDimensions.Create(userDimension);
                result = dbCreateResult != null ? true : false;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteUserDimension([FromBody] UserDimensionsViewModel data)
        {
            var userDimension = DBUserDimensions.GetById(data.UserId, data.Dimension, data.DimensionValue);
            return Json(userDimension != null ? DBUserDimensions.Delete(userDimension) : false);
        }

        [HttpPost]
        public JsonResult CreateUserAcessosLocalizacoes([FromBody] AcessosLocalizacoes data)
        {
            bool result = false;
            try
            {
                AcessosLocalizacoes userAcessosLocalizacoes = new AcessosLocalizacoes();
                userAcessosLocalizacoes.IdUtilizador = data.IdUtilizador;
                userAcessosLocalizacoes.Localizacao = data.Localizacao;
                userAcessosLocalizacoes.UtilizadorCriacao = User.Identity.Name;
                userAcessosLocalizacoes.DataHoraCriacao = DateTime.Now;

                var dbCreateResult = DBAcessosLocalizacoes.Create(userAcessosLocalizacoes);
                result = dbCreateResult != null ? true : false;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteUserAcessosLocalizacoes([FromBody] AcessosLocalizacoes data)
        {
            var userAcessosLocalizacoes = DBAcessosLocalizacoes.GetById(data.IdUtilizador, data.Localizacao);
            return Json(userAcessosLocalizacoes != null ? DBAcessosLocalizacoes.Delete(userAcessosLocalizacoes) : false);
        }

        [HttpPost]
        public JsonResult CreateUserAccess([FromBody] UserAccessesViewModel data)
        {
            bool result = false;
            try
            {
                AcessosUtilizador userAccess = new AcessosUtilizador();
                userAccess.IdUtilizador = data.IdUser;
                userAccess.Área = data.Area;
                userAccess.Funcionalidade = data.Feature;
                userAccess.Eliminação = data.Delete;
                userAccess.Inserção = data.Create;
                userAccess.Leitura = data.Read;
                userAccess.Modificação = data.Update;
                userAccess.UtilizadorCriação = User.Identity.Name;
                userAccess.DataHoraCriação = DateTime.Now;

                var dbCreateResult = DBUserAccesses.Create(userAccess);
                result = dbCreateResult != null ? true : false;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteUserProfile([FromBody] UserProfileViewModel data)
        {
            var userProfile = DBUserProfiles.GetById(data.UserId, data.Id);
            return Json(userProfile != null ? DBUserProfiles.Delete(userProfile) : false);
        }

        [HttpPost]
        public JsonResult CreateUserProfile([FromBody] UserProfileViewModel data)
        {
            bool result = false;
            try
            {
                PerfisUtilizador userProfile = new PerfisUtilizador();
                userProfile.IdUtilizador = data.UserId;
                userProfile.IdPerfil = data.Id;
                userProfile.UtilizadorCriação = User.Identity.Name;
                userProfile.DataHoraCriação = DateTime.Now;

                var dbCreateResult = DBUserProfiles.Create(userProfile);
                result = dbCreateResult != null ? true : false;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteUserAccess([FromBody] UserAccessesViewModel data)
        {
            var userAccess = DBUserAccesses.GetById(data.IdUser, data.Area, data.Feature);
            return Json(userAccess != null ? DBUserAccesses.Delete(userAccess) : false);
        }

        #endregion

        #region PerfisModelo
        public IActionResult PerfisModelo()
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetListProfileModels()
        {
            List<ProfileModelsViewModel> result = DBProfileModels.GetAll().Select(x => new ProfileModelsViewModel()
            {
                Id = x.Id,
                Description = x.Descrição
            }).ToList();
            return Json(result);
        }


        public IActionResult PerfisModeloDetalhes(int id)
        {
            ViewBag.ProfileModelId = id;

            return View();
        }

        [HttpPost]
        public JsonResult GetProfileModelData([FromBody] ProfileModelsViewModel data)
        {
            PerfisModelo PM = DBProfileModels.GetById(data.Id);
            ProfileModelsViewModel result = new ProfileModelsViewModel()
            {
                Id = 0,
                Description = "",
                ProfileModelAccesses = new List<AccessProfileModelView>()
            };

            if (PM != null)
            {
                result.Id = PM.Id;
                result.Description = PM.Descrição;

                result.ProfileModelAccesses = DBAccessProfiles.GetByProfileModelId(data.Id).Select(x => new AccessProfileModelView()
                {
                    IdProfile = x.IdPerfil,
                    Area = x.Área,
                    Feature = x.Funcionalidade,
                    Create = x.Inserção,
                    Read = x.Leitura,
                    Update = x.Modificação,
                    Delete = x.Eliminação
                }).ToList();
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateProfileModel([FromBody] ProfileModelsViewModel data)
        {
            PerfisModelo ObjectCreated = DBProfileModels.Create(new PerfisModelo()
            {
                Descrição = data.Description,
                UtilizadorCriação = User.Identity.Name
            });
            data.Id = ObjectCreated.Id;

            //Adicionar os acessos
            data.ProfileModelAccesses.ForEach(x =>
            {
                DBAccessProfiles.Create(new AcessosPerfil()
                {
                    IdPerfil = ObjectCreated.Id,
                    Área = x.Area,
                    Funcionalidade = x.Feature,
                    Inserção = x.Create,
                    Leitura = x.Read,
                    Modificação = x.Update,
                    Eliminação = x.Delete,
                    UtilizadorCriação = User.Identity.Name
                });
            });
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateProfileModel([FromBody] ProfileModelsViewModel data)
        {
            //Atualizar o elemento os acessos
            PerfisModelo PMObj = DBProfileModels.GetById(data.Id);
            PMObj.Descrição = data.Description;
            PMObj.UtilizadorModificação = User.Identity.Name;
            DBProfileModels.Update(PMObj);

            //Atualizar os acessos
            DBAccessProfiles.DeleteAllFromProfile(data.Id);
            data.ProfileModelAccesses.ForEach(x =>
            {
                DBAccessProfiles.Create(new AcessosPerfil()
                {
                    IdPerfil = data.Id,
                    Área = x.Area,
                    Funcionalidade = x.Feature,
                    Inserção = x.Create,
                    Leitura = x.Read,
                    Modificação = x.Update,
                    Eliminação = x.Delete,
                    UtilizadorCriação = User.Identity.Name
                });
            });
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteProfileModel([FromBody] ProfileModelsViewModel data)
        {
            PerfisModelo PMObj = DBProfileModels.GetById(data.Id);

            //Remover os acessos os acessos
            DBAccessProfiles.DeleteAllFromProfile(data.Id);

            if (DBProfileModels.Delete(PMObj))
            {
                data.Id = 0;
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteAccess([FromBody] AccessProfileModelView data)
        {
            if (data != null)
            {
                if (DBAccessProfiles.Delete(data.ParseToDB()))
                {
                    data.eReasonCode = 1;
                    data.eMessage = "Registo eliminado com sucesso.";
                }
                else
                {
                    data.eReasonCode = 2;
                    data.eMessage = "Ocorreu um erro ao eliminar o registo.";
                }
            }
            else
            {
                data = new AccessProfileModelView();
                data.eReasonCode = 2;
                data.eMessage = "Ocorreu um erro ao eliminar o registo.";
            }

            return Json(data);
        }
        #endregion

        public IActionResult Permicoes()
        {
            return View();
        }

        #region Configuracoes

        public IActionResult Configuracoes()
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetListConfigurations()
        {
            Configuração Cfg = DBConfigurations.GetById(1);

            ConfigurationsViewModel result = new ConfigurationsViewModel()
            {
                Id = Cfg.Id,
                ProjectNumeration = Cfg.NumeraçãoProjetos,
                ContractNumeration = Cfg.NumeraçãoContratos,
                TimeSheetNumeration = Cfg.NumeraçãoFolhasDeHoras,
                OportunitiesNumeration = Cfg.NumeraçãoOportunidades,
                ProposalsNumeration = Cfg.NumeraçãoPropostas,
                ContactsNumeration = Cfg.NumeraçãoContactos,
                DishesTechnicalSheetsNumeration = Cfg.NumeraçãoFichasTécnicasDePratos,
                PreRequisitionNumeration = Cfg.NumeraçãoPréRequisições,
                PurchasingProceduresNumeration = Cfg.NumeraçãoProcedimentoAquisição,
                RequisitionNumeration = Cfg.NumeraçãoRequisições,
                SimplifiedProceduresNumeration = Cfg.NumeraçãoProcedimentoSimplificado,
                SimplifiedReqTemplatesNumeration = Cfg.NumeraçãoModReqSimplificadas,
                SimplifiedRequisitionNumeration = Cfg.NumeraçãoRequisiçõesSimplificada,
                DinnerEndTime = Cfg.FimHoraJantar,
                DinnerStartTime = Cfg.InicioHoraJantar,
                LunchEndTime = Cfg.FimHoraAlmoco,
                LunchStartTime = Cfg.InicioHoraAlmoco
            };
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateConfigurations([FromBody] ConfigurationsViewModel data)
        {
            Configuração configObj = DBConfigurations.GetById(data.Id);


            if (configObj == null)
            {
                configObj.DataHoraCriação = DateTime.Now;
                configObj.UtilizadorCriação = User.Identity.Name;
            }

            configObj.NumeraçãoProjetos = data.ProjectNumeration;
            configObj.NumeraçãoContratos = data.ContractNumeration;
            configObj.NumeraçãoFolhasDeHoras = data.TimeSheetNumeration;
            configObj.NumeraçãoOportunidades = data.OportunitiesNumeration;
            configObj.NumeraçãoPropostas = data.ProposalsNumeration;
            configObj.NumeraçãoContactos = data.ContactsNumeration;
            configObj.NumeraçãoFichasTécnicasDePratos = data.DishesTechnicalSheetsNumeration;
            configObj.NumeraçãoPréRequisições = data.PreRequisitionNumeration;
            configObj.NumeraçãoProcedimentoAquisição = data.PurchasingProceduresNumeration;
            configObj.NumeraçãoRequisições = data.RequisitionNumeration;
            configObj.NumeraçãoProcedimentoSimplificado = data.SimplifiedProceduresNumeration;
            configObj.NumeraçãoModReqSimplificadas = data.SimplifiedReqTemplatesNumeration;
            configObj.NumeraçãoRequisiçõesSimplificada = data.SimplifiedRequisitionNumeration;
            configObj.FimHoraJantar = data.DinnerEndTime;
            configObj.InicioHoraJantar = data.DinnerStartTime;
            configObj.InicioHoraAlmoco = data.LunchStartTime;
            configObj.FimHoraAlmoco = data.LunchEndTime;

            configObj.UtilizadorModificação = User.Identity.Name;
            //configObj.UtilizadorCriação = User.Identity.Name;
            //configObj.DataHoraCriação = DateTime.Now;
            configObj.UtilizadorModificação = User.Identity.Name;
            configObj.DataHoraModificação = DateTime.Now;

            DBConfigurations.Update(configObj);

            return Json(data);
        }

        #endregion

        #region ConfiguracaoNumeracoes

        public IActionResult ConfiguracaoNumeracoes()
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetListConfigNumerations()
        {
            List<ConfigNumerationsViewModel> result = DBNumerationConfigurations.GetAll().Select(x => new ConfigNumerationsViewModel()
            {
                Id = x.Id,
                Auto = x.Automático,
                Manual = x.Manual,
                Prefix = x.Prefixo,
                Description = x.Descrição,
                TotalDigitIncrement = x.NºDígitosIncrementar,
                IncrementQuantity = x.QuantidadeIncrementar,
                LastNumerationUsed = x.ÚltimoNºUsado
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateNumerationConfigs([FromBody] List<ConfigNumerationsViewModel> data)
        {
            //Get All
            List<ConfiguraçãoNumerações> previousList = DBNumerationConfigurations.GetAll();
            //previousList.RemoveAll(x => !data.Any(u => u.Id == x.Id));
            //previousList.ForEach(x => DBNumerationConfigurations.Delete(x));

            foreach (ConfiguraçãoNumerações line in previousList)
            {
                if (!data.Any(x => x.Id == line.Id))
                {
                    DBNumerationConfigurations.Delete(line);
                }
            }

            data.ForEach(x =>
            {
                ConfiguraçãoNumerações CN = new ConfiguraçãoNumerações()
                {
                    Descrição = x.Description,
                    Automático = x.Auto,
                    Manual = x.Manual,
                    Prefixo = x.Prefix,
                    NºDígitosIncrementar = x.TotalDigitIncrement,
                    QuantidadeIncrementar = x.IncrementQuantity,
                    ÚltimoNºUsado = x.LastNumerationUsed
                };

                if (x.Id > 0)
                {
                    CN.Id = x.Id;
                    CN.UtilizadorModificação = User.Identity.Name;
                    CN.DataHoraModificação = DateTime.Now;
                    DBNumerationConfigurations.Update(CN);
                }
                else
                {
                    CN.UtilizadorCriação = User.Identity.Name;
                    CN.DataHoraCriação = DateTime.Now;
                    DBNumerationConfigurations.Create(CN);
                }
            });

            return Json(data);
        }
        #endregion

        #region TabelasAuxiliares

        #region TiposDeProjeto
        public IActionResult TiposProjetoDetalhes(string id)
        {

            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetProjectTypeData()
        {
            List<ProjectTypesModelView> result = DBProjectTypes.GetAll().Select(x => new ProjectTypesModelView()
            {
                Code = x.Código,
                Description = x.Descrição
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateProjectType([FromBody] List<ProjectTypesModelView> data)
        {
            List<TipoDeProjeto> results = DBProjectTypes.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código));
            results.ForEach(x => DBProjectTypes.Delete(x));
            data.ForEach(x =>
            {
                TipoDeProjeto tpval = new TipoDeProjeto()
                {
                    Descrição = x.Description
                };
                if (x.Code > 0)
                {
                    tpval.Código = x.Code;
                    tpval.DataHoraModificação = DateTime.Now;
                    tpval.UtilizadorModificação = User.Identity.Name;
                    DBProjectTypes.Update(tpval);
                }
                else
                {
                    tpval.DataHoraCriação = DateTime.Now;
                    tpval.UtilizadorCriação = User.Identity.Name;
                    DBProjectTypes.Create(tpval);
                }
            });
            return Json(data);
        }
        #endregion

        #region TiposGrupoContabProjeto
        public IActionResult TiposGrupoContabProjeto(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        //POPULATE GRID ContabGroupTypes
        public JsonResult GetTiposGrupoContabProjeto([FromBody] ContabGroupTypesProjectView data)
        {
            List<ContabGroupTypesProjectView> result = DBCountabGroupTypes.GetAll().Select(x => new ContabGroupTypesProjectView()
            {
                ID = x.Código,
                Description = x.Descrição,
                FunctionalAreaCode = x.CódigoÁreaFuncional,
                Region = x.CódigoRegião,
                ResponsabilityCenter = x.CódigoCentroResponsabilidade
            }).ToList();

            return Json(result);
        }

        //Create/Update/Delete 
        [HttpPost]
        public JsonResult UpdateTiposGrupoContabProjeto([FromBody] List<ContabGroupTypesProjectView> data)
        {
            //Get All
            List<TiposGrupoContabProjeto> previousList = DBCountabGroupTypes.GetAll();
            previousList.RemoveAll(x => data.Any(u => u.ID == x.Código));
            previousList.ForEach(x => DBCountabGroupTypes.DeleteAllFromProfile(x.Código));

            data.ForEach(x =>
            {
                TiposGrupoContabProjeto CN = new TiposGrupoContabProjeto()
                {
                    Descrição = x.Description,
                    CódigoCentroResponsabilidade = x.ResponsabilityCenter,
                    CódigoRegião = x.Region,
                    CódigoÁreaFuncional = x.FunctionalAreaCode
                };

                if (x.ID > 0)
                {
                    CN.DataHoraModificação = DateTime.Now;
                    CN.UtilizadorModificação = User.Identity.Name;
                    CN.Código = x.ID;
                    DBCountabGroupTypes.Update(CN);
                }
                else
                {
                    CN.UtilizadorCriação = User.Identity.Name;
                    CN.DataHoraCriação = DateTime.Now;
                    DBCountabGroupTypes.Create(CN);
                }
            });

            return Json(data);
        }
        #endregion

        #region ObjetosDeServiço

        public IActionResult ObjetosDeServico(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetServiceObjectsData()
        {
            List<ServiceObjectsViewModel> result = DBServiceObjects.GetAll().Select(x => new ServiceObjectsViewModel()
            {
                Code = x.Código,
                Description = x.Descrição,
                Blocked = x.Bloqueado,
                AreaCode = x.CódÁrea
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateServiceObjects([FromBody] List<ServiceObjectsViewModel> data)
        {
            List<ObjetosDeServiço> results = DBServiceObjects.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código));
            results.ForEach(x => DBServiceObjects.Delete(x));
            data.ForEach(x =>
            {
                ObjetosDeServiço OS = new ObjetosDeServiço()
                {
                    Descrição = x.Description,
                    Bloqueado = x.Blocked,
                    CódÁrea = x.AreaCode
                };
                if (x.Code > 0)
                {
                    OS.Código = x.Code;
                    OS.DataHoraModificação = DateTime.Now;
                    OS.UtilizadorModificação = User.Identity.Name;
                    DBServiceObjects.Update(OS);
                }
                else
                {

                    OS.DataHoraCriação = DateTime.Now;
                    OS.UtilizadorCriação = User.Identity.Name;
                    DBServiceObjects.Create(OS);
                }
            });
            return Json(data);
        }
        #endregion

        #region TiposGrupoContabOMProjeto

        public IActionResult TiposGrupoContabOMProjeto(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public JsonResult GetTiposGrupoContabOMProjeto([FromBody] ContabGroupTypesOMProjectViewModel data)
        {
            List<ContabGroupTypesOMProjectViewModel> result = DBCountabGroupTypesOM.GetAll().Select(x => new ContabGroupTypesOMProjectViewModel()
            {
                Code = x.Código,
                Type = x.Tipo,
                Description = x.Descrição,
                CorrectiveMaintenance = x.ManutCorretiva,
                PreventiveMaintenance = x.ManutPreventiva,
                FailType = x.TipoRazãoFalha,
                ResponseTimeIndicator = x.IndicadorTempoResposta,
                StopTimeIndicator = x.IndicadorTempoImobilização,
                RepairEffectiveTimeIndicator = x.IndicadorTempoEfetivoReparação,
                ClosingWorksTimeIndicator = x.IndicadorTempoFechoObras,
                BillingTimeIndicator = x.IndicadorTempoFaturação,
                EmployeesOccupationTimeIndicator = x.IndicadorTempoOcupColaboradores,
                CostSaleValueIndicator = x.IndicadorValorCustoVenda,
                CATComplianceRateIndicator = x.IndicTaxaCumprimentoCat,
                CATCoverageRateIndicator = x.IndicadorTaxaCoberturaCat,
                MPRoutineFulfillmentRateIndicator = x.IndicTaxaCumprRotinasMp,
                BreakoutIncidentsIndicator = x.IndicIncidênciasAvarias,
                OrdernInProgressIndicator = x.IndicadorOrdensEmCurso
            }).ToList();

            return Json(result);
        }

        //Create/Update/Delete 
        [HttpPost]
        public JsonResult UpdateTiposGrupoContabProjetoOM([FromBody] List<ContabGroupTypesOMProjectViewModel> data)
        {
            //Get All
            List<TiposGrupoContabOmProjeto> previousList = DBCountabGroupTypesOM.GetAll();
            previousList.RemoveAll(x => data.Any(u => u.Code == x.Código));
            previousList.ForEach(x => DBCountabGroupTypesOM.DeleteAllFromProfile(x));
            data.ForEach(x =>
            {
                TiposGrupoContabOmProjeto CN = new TiposGrupoContabOmProjeto()
                {
                    Código = x.Code,
                    Tipo = x.Type,
                    Descrição = x.Description,
                    ManutCorretiva = x.CorrectiveMaintenance,
                    ManutPreventiva = x.PreventiveMaintenance,
                    TipoRazãoFalha = x.FailType,
                    IndicadorTempoResposta = x.ResponseTimeIndicator,
                    IndicadorTempoImobilização = x.StopTimeIndicator,
                    IndicadorTempoEfetivoReparação = x.RepairEffectiveTimeIndicator,
                    IndicadorTempoFechoObras = x.ClosingWorksTimeIndicator,
                    IndicadorTempoFaturação = x.BillingTimeIndicator,
                    IndicadorTempoOcupColaboradores = x.EmployeesOccupationTimeIndicator,
                    IndicadorValorCustoVenda = x.CostSaleValueIndicator,
                    IndicTaxaCumprimentoCat = x.CATComplianceRateIndicator,
                    IndicadorTaxaCoberturaCat = x.CATCoverageRateIndicator,
                    IndicTaxaCumprRotinasMp = x.MPRoutineFulfillmentRateIndicator,
                    IndicIncidênciasAvarias = x.BreakoutIncidentsIndicator,
                    IndicadorOrdensEmCurso = x.OrdernInProgressIndicator
                };

                if (x.Code > 0)
                {
                    CN.DataHoraModificação = DateTime.Now;
                    CN.UtilizadorModificação = User.Identity.Name;
                    CN.Código = x.Code;
                    DBCountabGroupTypesOM.Update(CN);
                }
                else
                {
                    CN.UtilizadorCriação = User.Identity.Name;
                    CN.DataHoraCriação = DateTime.Now;
                    DBCountabGroupTypesOM.Create(CN);
                }
            });

            return Json(data);
        }
        #endregion TiposGrupoContabOMProjeto

        #region TiposRefeicao
        public IActionResult TiposRefeicao(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetMealTypesData()
        {
            List<MealTypesViewModel> result = DBMealTypes.GetAll().Select(x => new MealTypesViewModel()
            {
                Code = x.Código,
                Description = x.Descrição,
                GrupoContabProduto = x.GrupoContabProduto
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateMealTypes([FromBody] List<MealTypesViewModel> data)
        {
            List<TiposRefeição> results = DBMealTypes.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código));
            results.ForEach(x => DBMealTypes.Delete(x));
            data.ForEach(x =>
            {
                TiposRefeição TR = new TiposRefeição()
                {
                    Descrição = x.Description,
                    GrupoContabProduto = x.GrupoContabProduto
                };
                if (x.Code > 0)
                {
                    TR.Código = x.Code;
                    TR.DataHoraModificação = DateTime.Now;
                    TR.UtilizadorModificação = User.Identity.Name;
                    DBMealTypes.Update(TR);
                }
                else
                {
                    TR.DataHoraCriação = DateTime.Now;
                    TR.UtilizadorCriação = User.Identity.Name;
                    DBMealTypes.Create(TR);
                }
            });
            return Json(data);
        }


        #endregion

        #region DestinosFinaisResiduos
        public IActionResult DestinosFinaisResiduos(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetFinalWasteDestinationsData()
        {
            List<FinalWasteDestinationsViewModel> result = DBFinalWasteDestinations.GetAll().Select(x => new FinalWasteDestinationsViewModel()
            {
                Code = x.Código,
                Description = x.Descrição
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateFinalWasteDestinations([FromBody] List<FinalWasteDestinationsViewModel> data)
        {
            List<DestinosFinaisResíduos> results = DBFinalWasteDestinations.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código));
            results.ForEach(x => DBFinalWasteDestinations.Delete(x));
            data.ForEach(x =>
            {
                DestinosFinaisResíduos DFR = new DestinosFinaisResíduos()
                {
                    Descrição = x.Description
                };
                if (x.Code > 0)
                {
                    DFR.Código = x.Code;
                    DFR.DataHoraModificação = DateTime.Now;
                    DFR.UtilizadorModificação = User.Identity.Name;
                    DBFinalWasteDestinations.Update(DFR);
                }
                else
                {
                    DFR.DataHoraCriação = DateTime.Now;
                    DFR.UtilizadorCriação = User.Identity.Name;
                    DBFinalWasteDestinations.Create(DFR);
                }
            });
            return Json(data);
        }


        #endregion

        #region Serviço
        public IActionResult Servicos(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetServices()
        {
            List<ProjectTypesModelView> result = DBServices.GetAll().Select(x => new ProjectTypesModelView()
            {
                Code = x.Código,
                Description = x.Descrição
            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateServices([FromBody] List<ProjectTypesModelView> data)
        {
            List<Serviços> results = DBServices.GetAll();
            results.RemoveAll(x => data.Any(u => u.Code == x.Código));
            results.ForEach(x => DBServices.Delete(x.Código));
            data.ForEach(x =>
            {
                Serviços tpval = new Serviços()
                {
                    Descrição = x.Description
                };
                if (x.Code > 0)
                {
                    tpval.DataHoraModificação = DateTime.Now;
                    tpval.UtilizadorModificação = User.Identity.Name;
                    tpval.Código = x.Code;
                    DBServices.Update(tpval);
                }
                else
                {
                    tpval.UtilizadorCriação = User.Identity.Name;
                    tpval.DataHoraCriação = DateTime.Now;
                    DBServices.Create(tpval);
                }
            });
            return Json(data);
        }
        #endregion

        #region ServiçosCliente
        public IActionResult ServicosCliente(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetClientServices()
        {
            List<ClientServicesViewModel> result = DBClientServices.GetAll().Select(x => new ClientServicesViewModel()
            {
                ClientNumber = x.NºCliente,
                ServiceCode = x.CódServiço,
                ServiceGroup = x.GrupoServiços
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateClientServices([FromBody] List<ClientServicesViewModel> data)
        {
            foreach (var dt in data)
            {
                int param = 2;
                bool exist = CheckIfExist(dt.ClientNumber, dt.ServiceCode, dt.ServiceGroup, param);
                if (exist == false)
                {
                    ServiçosCliente tpval = new ServiçosCliente();
                    tpval.UtilizadorModificação = User.Identity.Name;
                    tpval.DataHoraModificação = DateTime.Now;
                    tpval.GrupoServiços = dt.ServiceGroup;
                    tpval.CódServiço = dt.ServiceCode;
                    tpval.NºCliente = dt.ClientNumber;

                    DBClientServices.Update(tpval);
                }
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult CreateClientServices([FromBody] List<ClientServicesViewModel> data)
        {
            try
            {

                int totalExists = 0;
                if (data != null)
                {
                    foreach (var dt in data)
                    {
                        int param = 1;
                        bool exist = CheckIfExist(dt.ClientNumber, dt.ServiceCode, dt.ServiceGroup, param);
                        if (exist == false)
                        {
                            ServiçosCliente tpval = new ServiçosCliente();
                            tpval.UtilizadorCriação = User.Identity.Name;
                            tpval.DataHoraCriação = DateTime.Now;
                            tpval.GrupoServiços = dt.ServiceGroup;
                            tpval.CódServiço = dt.ServiceCode;
                            tpval.NºCliente = dt.ClientNumber;

                            DBClientServices.Create(tpval);
                        }
                        else
                        {
                            totalExists++;
                        }
                    }
                }
                if (totalExists == data.Count())
                {
                    return Json(true);
                }
                return Json(false);
            }
            catch (Exception)
            {
                return Json(false);
            }
        }

        [HttpPost]
        public JsonResult DeleteClientServices([FromBody] List<ClientServicesViewModel> data)
        {
            try
            {
                List<ServiçosCliente> results = DBClientServices.GetAll();
                results.RemoveAll(x => data.Any(u => u.ClientNumber == x.NºCliente && u.ServiceCode == x.CódServiço));
                results.ForEach(x => DBClientServices.Delete(x.CódServiço, x.NºCliente));
                return Json(data);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool CheckIfExist(string ClientNumber, int ServiceCode, bool? ServiceGroup, int param)
        {
            List<ClientServicesViewModel> result = DBClientServices.GetAll().Select(x => new ClientServicesViewModel()
            {
                ClientNumber = x.NºCliente,
                ServiceCode = x.CódServiço,
                ServiceGroup = x.GrupoServiços
            }).ToList();

            bool exists = false;
            if (param == 1)
            {
                foreach (var res in result)
                {
                    if (res.ClientNumber == ClientNumber && res.ServiceCode == ServiceCode)
                    {
                        exists = true;
                    }
                }
            }

            if (param == 2)
            {
                foreach (var res in result)
                {
                    if (res.ClientNumber == ClientNumber && res.ServiceCode == ServiceCode && res.ServiceGroup == ServiceGroup)
                    {
                        exists = true;
                    }
                }
            }
            return exists;
        }
        #endregion

        #region TiposViaturas
        public IActionResult TiposViaturas(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetTiposViaturas()
        {
            List<TiposViaturaViewModel> result = DBTiposViaturas.ParseListToViewModel(DBTiposViaturas.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateTiposViaturas([FromBody] TiposViaturaViewModel data)
        {
            TiposViatura tiposViatura = DBTiposViaturas.ParseToDB(data);
            tiposViatura.UtilizadorCriação = User.Identity.Name;
            DBTiposViaturas.Create(tiposViatura);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteTiposViaturas([FromBody] TiposViaturaViewModel data)
        {
            var result = DBTiposViaturas.Delete(DBTiposViaturas.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateTiposViaturas([FromBody] List<TiposViaturaViewModel> data)
        {
            List<TiposViatura> results = DBTiposViaturas.GetAll();
            data.RemoveAll(x => results.Any(u => u.CódigoTipo == x.CodigoTipo && u.Descrição == x.Descricao));

            data.ForEach(x =>
            {
                TiposViatura tiposViatura = DBTiposViaturas.ParseToDB(x);
                tiposViatura.UtilizadorModificação = User.Identity.Name;
                DBTiposViaturas.Update(tiposViatura);
            });
            return Json(data);
        }


        #endregion

        #region Marcas
        public IActionResult Marcas(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetMarcas()
        {
            List<MarcasViewModel> result = DBMarcas.ParseListToViewModel(DBMarcas.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateMarca([FromBody] MarcasViewModel data)
        {
            Marcas toCreate = DBMarcas.ParseToDB(data);
            toCreate.UtilizadorCriação = User.Identity.Name;
            DBMarcas.Create(toCreate);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteMarca([FromBody] MarcasViewModel data)
        {
            var result = DBMarcas.Delete(DBMarcas.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateMarcas([FromBody] List<MarcasViewModel> data)
        {
            List<Marcas> results = DBMarcas.GetAll();
            data.RemoveAll(x => results.Any(u => u.CódigoMarca == x.CodigoMarca && u.Descrição == x.Descricao));

            data.ForEach(x =>
            {
                Marcas toUpdate = DBMarcas.ParseToDB(x);
                toUpdate.UtilizadorModificação = User.Identity.Name;
                DBMarcas.Update(toUpdate);
            });
            return Json(data);
        }

        #endregion

        #region Modelos
        public IActionResult Modelos(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetModelos()
        {
            List<ModelosViewModel> result = DBModelos.ParseListToViewModel(DBModelos.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateModelo([FromBody] ModelosViewModel data)
        {
            Modelos toCreate = DBModelos.ParseToDB(data);
            toCreate.UtilizadorCriação = User.Identity.Name;
            DBModelos.Create(toCreate);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteModelo([FromBody] ModelosViewModel data)
        {
            var result = DBModelos.Delete(DBModelos.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateModelos([FromBody] List<ModelosViewModel> data)
        {
            List<Modelos> results = DBModelos.GetAll();
            data.RemoveAll(x => results.Any(u => u.CódigoModelo == x.CodigoModelo && u.Descrição == x.Descricao));

            data.ForEach(x =>
            {
                Modelos toUpdate = DBModelos.ParseToDB(x);
                toUpdate.UtilizadorModificação = User.Identity.Name;
                DBModelos.Update(toUpdate);
            });
            return Json(data);
        }

        #endregion

        #region Cartoes E Apolices
        public IActionResult CartoesEApolices(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetCartoesEApolices()
        {
            List<CartoesEApolicesViewModel> result = DBCartoesEApolices.ParseListToViewModel(DBCartoesEApolices.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateCartoesEApolices([FromBody] CartoesEApolicesViewModel data)
        {
            CartõesEApólices toCreate = DBCartoesEApolices.ParseToDB(data);
            toCreate.UtilizadorCriação = User.Identity.Name;
            DBCartoesEApolices.Create(toCreate);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteCartoesEApolices([FromBody] CartoesEApolicesViewModel data)
        {
            var result = DBCartoesEApolices.Delete(DBCartoesEApolices.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateCArtoesEApolices([FromBody] List<CartoesEApolicesViewModel> data)
        {
            List<CartõesEApólices> results = DBCartoesEApolices.GetAll();

            data.RemoveAll(x => DBCartoesEApolices.ParseListToViewModel(results).Any(
                u =>
                    u.Tipo == x.Tipo &&
                    u.Numero == x.Numero &&
                    u.Descricao == x.Descricao &&
                    u.DataInicio == x.DataInicio &&
                    u.DataFim == x.DataFim &&
                    u.Fornecedor == x.Fornecedor
            ));

            data.ForEach(x =>
            {
                CartõesEApólices toUpdate = DBCartoesEApolices.ParseToDB(x);
                toUpdate.UtilizadorModificação = User.Identity.Name;
                DBCartoesEApolices.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region Configuracao Ajuda De Custo
        public IActionResult ConfiguracaoAjudaCusto(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetConfiguracaoAjudaCusto()
        {
            List<ConfiguracaoAjudaCustoViewModel> result = DBConfiguracaoAjudaCusto.ParseListToViewModel(DBConfiguracaoAjudaCusto.GetAll());

            if (result != null)
            {
                result.ForEach(x =>
                {
                    x.CodigoTipoCustoTexto = x.CodigoTipoCusto.Trim() + " - " + DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.CodigoTipoCusto.Trim(), "", 0, "").FirstOrDefault().Name;
                });
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateConfiguracaoAjudaCusto([FromBody] ConfiguracaoAjudaCustoViewModel data)
        {

            ConfiguracaoAjudaCusto toCreate = DBConfiguracaoAjudaCusto.ParseToDB(data);
            toCreate.UtilizadorCriacao = User.Identity.Name;
            var result = DBConfiguracaoAjudaCusto.Create(toCreate);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteConfiguracaoAjudaCusto([FromBody] ConfiguracaoAjudaCustoViewModel data)
        {
            var result = DBConfiguracaoAjudaCusto.Delete(DBConfiguracaoAjudaCusto.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateConfiguracaoAjudaCusto([FromBody] List<ConfiguracaoAjudaCustoViewModel> data)
        {
            List<ConfiguracaoAjudaCusto> results = DBConfiguracaoAjudaCusto.GetAll();

            data.RemoveAll(x => DBConfiguracaoAjudaCusto.ParseListToViewModel(results).Any(
                u =>
                    u.CodigoTipoCusto == x.CodigoTipoCusto &&
                    u.DistanciaMinima == x.DistanciaMinima &&
                    u.DataChegadaDataPartida == x.DataChegadaDataPartida &&
                    u.LimiteHoraPartida == x.LimiteHoraPartida &&
                    u.LimiteHoraChegada == x.LimiteHoraChegada &&
                    u.Prioritario == x.Prioritario &&
                    u.TipoCusto == x.TipoCusto &&
                    u.CodigoRefCusto == x.CodigoRefCusto &&
                    u.SinalHoraPartida == x.SinalHoraPartida &&
                    u.HoraPartida == x.HoraPartida &&
                    u.SinalHoraChegada == x.SinalHoraChegada &&
                    u.HoraChegada == x.HoraChegada
            ));

            data.ForEach(x =>
            {
                ConfiguracaoAjudaCusto toUpdate = DBConfiguracaoAjudaCusto.ParseToDB(x);
                toUpdate.UtilizadorModificacao = User.Identity.Name;
                DBConfiguracaoAjudaCusto.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region Configuracao Tipo Trabalho FH
        public IActionResult ConfiguracaoTipoTrabalhoFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetConfiguracaoTipoTrabalhoFH()
        {
            List<TipoTrabalhoFHViewModel> result = DBTipoTrabalhoFH.ParseListToViewModel(DBTipoTrabalhoFH.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateConfiguracaoTipoTrabalhoFH([FromBody] TipoTrabalhoFHViewModel data)
        {
            int resultFinal = 0;

            TipoTrabalhoFh toCreate = DBTipoTrabalhoFH.ParseToDB(data);
            toCreate.CriadoPor = User.Identity.Name;
            var result = DBTipoTrabalhoFH.Create(toCreate);

            if (result == null)
                resultFinal = 0;
            else
                resultFinal = 1;
            //return Json(data);
            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteTipoTrabalhoFH([FromBody] TipoTrabalhoFHViewModel data)
        {
            var result = DBTipoTrabalhoFH.Delete(DBTipoTrabalhoFH.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateTipoTrabalhoFH([FromBody] List<TipoTrabalhoFHViewModel> data)
        {
            List<TipoTrabalhoFh> results = DBTipoTrabalhoFH.GetAll();

            data.RemoveAll(x => DBTipoTrabalhoFH.ParseListToViewModel(results).Any(
                u =>
                    u.Codigo == x.Codigo &&
                    u.Descricao == x.Descricao &&
                    u.CodUnidadeMedida == x.CodUnidadeMedida &&
                    u.HoraViagem == x.HoraViagem &&
                    u.TipoHora == x.TipoHora &&
                    u.UtilizadorCriacao == x.UtilizadorCriacao &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                TipoTrabalhoFh toUpdate = DBTipoTrabalhoFH.ParseToDB(x);
                toUpdate.AlteradoPor = User.Identity.Name;
                DBTipoTrabalhoFH.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region Configuração Preço Venda Recursos FH
        public IActionResult ConfiguracaoPrecoVendaRecursoFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetConfiguracaoPrecoVendaRecursoFH()
        {
            List<PrecoVendaRecursoFHViewModel> result = DBPrecoVendaRecursoFH.ParseListToViewModel(DBPrecoVendaRecursoFH.GetAll());

            if (result != null)
            {
                result.ForEach(x =>
                {
                    x.Descricao = x.Code + " - " + DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.Code, "", 0, "").FirstOrDefault().Name;
                    x.CodTipoTrabalhoTexto = x.CodTipoTrabalho + " - " + DBTipoTrabalhoFH.GetAll().Where(y => y.Codigo == x.CodTipoTrabalho).FirstOrDefault().Descricao;
                    x.FamiliaRecurso = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.Code, "", 0, "").FirstOrDefault().ResourceGroup;
                });
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateConfiguracaoPrecoVendaRecursoFH([FromBody] PrecoVendaRecursoFHViewModel data)
        {
            int resultFinal = 0;

            PrecoVendaRecursoFh toCreate = DBPrecoVendaRecursoFH.ParseToDB(data);

            NAVResourcesViewModel resource = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, data.Code, "", 0, "").FirstOrDefault();

            toCreate.Descricao = resource.Name;
            toCreate.FamiliaRecurso = resource.ResourceGroup;

            toCreate.CriadoPor = User.Identity.Name;
            var result = DBPrecoVendaRecursoFH.Create(toCreate);

            if (result == null)
                resultFinal = 0;
            else
                resultFinal = 1;

            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeletePrecoVendaRecursoFH([FromBody] PrecoVendaRecursoFHViewModel data)
        {
            var result = DBPrecoVendaRecursoFH.Delete(DBPrecoVendaRecursoFH.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdatePrecoVendaRecursoFH([FromBody] List<PrecoVendaRecursoFHViewModel> data)
        {
            List<PrecoVendaRecursoFh> results = DBPrecoVendaRecursoFH.GetAll();

            data.RemoveAll(x => DBPrecoVendaRecursoFH.ParseListToViewModel(results).Any(
                u =>
                    u.Code == x.Code &&
                    u.Descricao == x.Descricao &&
                    u.CodTipoTrabalho == x.CodTipoTrabalho &&
                    u.PrecoUnitario == x.PrecoUnitario &&
                    u.CustoUnitario == x.CustoUnitario &&
                    u.StartingDate == x.StartingDate &&
                    u.EndingDate == x.EndingDate &&
                    u.FamiliaRecurso == x.FamiliaRecurso &&
                    u.UtilizadorCriacao == x.UtilizadorCriacao &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                PrecoVendaRecursoFh toUpdate = DBPrecoVendaRecursoFH.ParseToDB(x);
                toUpdate.Descricao = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.Code, "", 0, "").FirstOrDefault().Name;
                toUpdate.FamiliaRecurso = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.Code, "", 0, "").FirstOrDefault().ResourceGroup;
                toUpdate.AlteradoPor = User.Identity.Name;
                DBPrecoVendaRecursoFH.Update(toUpdate);
            });
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetRecurso([FromBody] NAVResourcesViewModel data)
        {
            NAVResourcesViewModel result = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, data.Code, "", 0, "").FirstOrDefault();

            return Json(result);
        }
        #endregion

        #region Configuração Preço Custo Recursos FH
        public IActionResult ConfiguracaoPrecoCustoRecursoFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetConfiguracaoPrecoCustoRecursoFH()
        {
            List<PrecoCustoRecursoViewModel> result = DBPrecoCustoRecursoFH.ParseListToViewModel(DBPrecoCustoRecursoFH.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateConfiguracaoPrecoCustoRecursoFH([FromBody] PrecoCustoRecursoViewModel data)
        {

            PrecoCustoRecursoFh toCreate = DBPrecoCustoRecursoFH.ParseToDB(data);
            toCreate.CriadoPor = User.Identity.Name;
            var result = DBPrecoCustoRecursoFH.Create(toCreate);

            return Json(data);
        }

        [HttpPost]
        public JsonResult DeletePrecoCustoRecursoFH([FromBody] PrecoCustoRecursoViewModel data)
        {
            var result = DBPrecoCustoRecursoFH.Delete(DBPrecoCustoRecursoFH.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdatePrecoCustoRecursoFH([FromBody] List<PrecoCustoRecursoViewModel> data)
        {
            List<PrecoCustoRecursoFh> results = DBPrecoCustoRecursoFH.GetAll();

            data.RemoveAll(x => DBPrecoCustoRecursoFH.ParseListToViewModel(results).Any(
                u =>
                    u.Code == x.Code &&
                    u.Descricao == x.Descricao &&
                    u.CodTipoTrabalho == x.CodTipoTrabalho &&
                    u.CustoUnitario == x.CustoUnitario &&
                    u.StartingDate == x.StartingDate &&
                    u.EndingDate == x.EndingDate &&
                    u.FamiliaRecurso == x.FamiliaRecurso &&
                    u.UtilizadorCriacao == x.UtilizadorCriacao &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                PrecoCustoRecursoFh toUpdate = DBPrecoCustoRecursoFH.ParseToDB(x);
                toUpdate.AlteradoPor = User.Identity.Name;
                DBPrecoCustoRecursoFH.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region Configuração RH Recursos FH
        public IActionResult ConfiguracaoRHRecursosFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult ConfiguracaoAutorizacaoFHRH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetRHRecursosFH()
        {
            List<RHRecursosViewModel> result = DBRHRecursosFH.ParseListToViewModel(DBRHRecursosFH.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateRHRecursosFH([FromBody] RHRecursosViewModel data)
        {
            int resultFinal = 0;

            RhRecursosFh toCreate = DBRHRecursosFH.ParseToDB(data);

            NAVResourcesViewModel resource = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, data.Recurso, "", 0, "").FirstOrDefault();
            NAVEmployeeViewModel employee = DBNAV2009Employees.GetAll(data.NoEmpregado, _config.NAV2009DatabaseName, _config.NAV2009CompanyName).FirstOrDefault();

            toCreate.NomeRecurso = resource.Name;
            toCreate.FamiliaRecurso = resource.ResourceGroup;
            toCreate.NomeEmpregado = employee.Name;
            toCreate.CriadoPor = User.Identity.Name;

            var result = DBRHRecursosFH.Create(toCreate);

            if (result == null)
                resultFinal = 0;
            else
                resultFinal = 1;

            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteRHRecursosFH([FromBody] RHRecursosViewModel data)
        {
            var result = DBRHRecursosFH.Delete(DBRHRecursosFH.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateRHRecursosFH([FromBody] List<RHRecursosViewModel> data)
        {
            List<RhRecursosFh> results = DBRHRecursosFH.GetAll();

            data.RemoveAll(x => DBRHRecursosFH.ParseListToViewModel(results).Any(
                u =>
                    u.NoEmpregado == x.NoEmpregado &&
                    u.Recurso == x.Recurso &&
                    u.NomeRecurso == x.NomeRecurso &&
                    u.FamiliaRecurso == x.FamiliaRecurso &&
                    u.NomeEmpregado == x.NomeEmpregado &&
                    u.UtilizadorCriacao == x.UtilizadorCriacao &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                RhRecursosFh toUpdate = DBRHRecursosFH.ParseToDB(x);

                NAVResourcesViewModel resource = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.Recurso, "", 0, "").FirstOrDefault();
                toUpdate.NomeRecurso = resource.Name;
                toUpdate.FamiliaRecurso = resource.ResourceGroup;

                toUpdate.AlteradoPor = User.Identity.Name;
                DBRHRecursosFH.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region AutorizacaoFHRH
        [HttpPost]
        public JsonResult GetAutorizacaoFHRH()
        {
            List<AutorizacaoFHRHViewModel> result = DBAutorizacaoFHRH.ParseListToViewModel(DBAutorizacaoFHRH.GetAll());

            result.ForEach(x =>
            {
                x.NomeEmpregado = DBUserConfigurations.GetById(x.NoEmpregado) == null ? "" : DBUserConfigurations.GetById(x.NoEmpregado).Nome;
                x.NomeResponsavel1 = DBUserConfigurations.GetById(x.NoResponsavel1) == null ? "" : DBUserConfigurations.GetById(x.NoResponsavel1).Nome;
                x.NomeResponsavel2 = DBUserConfigurations.GetById(x.NoResponsavel2) == null ? "" : DBUserConfigurations.GetById(x.NoResponsavel2).Nome;
                x.NomeResponsavel3 = DBUserConfigurations.GetById(x.NoResponsavel3) == null ? "" : DBUserConfigurations.GetById(x.NoResponsavel3).Nome;
                x.NomeValidadorRH1 = DBUserConfigurations.GetById(x.ValidadorRH1) == null ? "" : DBUserConfigurations.GetById(x.ValidadorRH1).Nome;
                x.NomeValidadorRH2 = DBUserConfigurations.GetById(x.ValidadorRH2) == null ? "" : DBUserConfigurations.GetById(x.ValidadorRH2).Nome;
                x.NomeValidadorRH3 = DBUserConfigurations.GetById(x.ValidadorRH3) == null ? "" : DBUserConfigurations.GetById(x.ValidadorRH3).Nome;
                x.NomeValidadorRHKM1 = DBUserConfigurations.GetById(x.ValidadorRHKM1) == null ? "" : DBUserConfigurations.GetById(x.ValidadorRHKM1).Nome;
                x.NomeValidadorRHKM2 = DBUserConfigurations.GetById(x.ValidadorRHKM2) == null ? "" : DBUserConfigurations.GetById(x.ValidadorRHKM2).Nome;
            });
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateAutorizacaoFHRH([FromBody] AutorizacaoFHRHViewModel data)
        {
            int resultFinal = 0;
            try
            {
                AutorizacaoFhRh autorizacao = new AutorizacaoFhRh();

                autorizacao.NoEmpregado = data.NoEmpregado;
                autorizacao.NoResponsavel1 = data.NoResponsavel1;
                autorizacao.NoResponsavel2 = data.NoResponsavel2;
                autorizacao.NoResponsavel3 = data.NoResponsavel3;
                autorizacao.ValidadorRh1 = data.ValidadorRH1;
                autorizacao.ValidadorRh2 = data.ValidadorRH2;
                autorizacao.ValidadorRh3 = data.ValidadorRH3;
                autorizacao.ValidadorRhkm1 = data.ValidadorRHKM1;
                autorizacao.ValidadorRhkm2 = data.ValidadorRHKM2;
                autorizacao.CriadoPor = User.Identity.Name;
                autorizacao.DataHoraCriação = DateTime.Now;

                var dbCreateResult = DBAutorizacaoFHRH.Create(autorizacao);

                if (dbCreateResult == null)
                    resultFinal = 0;
                else
                    resultFinal = 1;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteAutorizacaoFHRH([FromBody] AutorizacaoFHRHViewModel data)
        {
            var result = DBAutorizacaoFHRH.Delete(DBAutorizacaoFHRH.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateAutorizacaoFHRH([FromBody] List<AutorizacaoFHRHViewModel> data)
        {
            List<AutorizacaoFhRh> results = DBAutorizacaoFHRH.GetAll();

            data.RemoveAll(x => DBAutorizacaoFHRH.ParseListToViewModel(results).Any(
                u =>
                    u.NoEmpregado == x.NoEmpregado &&
                    u.NoResponsavel1 == x.NoResponsavel1 &&
                    u.NoResponsavel2 == x.NoResponsavel2 &&
                    u.NoResponsavel3 == x.NoResponsavel3 &&
                    u.ValidadorRH1 == x.ValidadorRH1 &&
                    u.ValidadorRH2 == x.ValidadorRH2 &&
                    u.ValidadorRH3 == x.ValidadorRH3 &&
                    u.ValidadorRHKM1 == x.ValidadorRHKM1 &&
                    u.ValidadorRHKM2 == x.ValidadorRHKM2 &&
                    u.UtilizadorCriacao == x.UtilizadorCriacao &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                AutorizacaoFhRh toUpdate = DBAutorizacaoFHRH.ParseToDB(x);
                toUpdate.AlteradoPor = User.Identity.Name;
                DBAutorizacaoFHRH.Update(toUpdate);
            });
            return Json(data);
        }

        #endregion

        #region OrigemDestinoFH
        public IActionResult ConfiguracaoOrigemDestinoFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetOrigemDestinoFH()
        {
            List<OrigemDestinoFHViewModel> result = DBOrigemDestinoFh.ParseListToViewModel(DBOrigemDestinoFh.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateOrigemDestinoFH([FromBody] OrigemDestinoFHViewModel data)
        {
            int resultFinal = 0;
            try
            {
                OrigemDestinoFh OrigemDestinoFH = new OrigemDestinoFh();

                OrigemDestinoFH.Código = data.Codigo;
                OrigemDestinoFH.Descrição = data.Descricao;
                OrigemDestinoFH.CriadoPor = User.Identity.Name;
                OrigemDestinoFH.DataHoraCriação = DateTime.Now;

                var dbCreateResult = DBOrigemDestinoFh.Create(OrigemDestinoFH);

                if (dbCreateResult == null)
                    resultFinal = 0;
                else
                    resultFinal = 1;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteOrigemDestinoFH([FromBody] OrigemDestinoFHViewModel data)
        {
            var result = DBOrigemDestinoFh.Delete(DBOrigemDestinoFh.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateOrigemDestinoFH([FromBody] List<OrigemDestinoFHViewModel> data)
        {
            List<OrigemDestinoFh> results = DBOrigemDestinoFh.GetAll();

            data.RemoveAll(x => DBOrigemDestinoFh.ParseListToViewModel(results).Any(
                u =>
                    u.Codigo == x.Codigo &&
                    u.Descricao == x.Descricao &&
                    u.CriadoPor == x.CriadoPor &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                OrigemDestinoFh toUpdate = DBOrigemDestinoFh.ParseToDB(x);
                toUpdate.AlteradoPor = User.Identity.Name;
                DBOrigemDestinoFh.Update(toUpdate);
            });
            return Json(data);
        }

        #endregion

        #region DistanciaFH
        public IActionResult ConfiguracaoDistanciaFH(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetDistanciaFH()
        {
            List<DistanciaFHViewModel> result = DBDistanciaFh.ParseListToViewModel(DBDistanciaFh.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateDistanciaFH([FromBody] DistanciaFHViewModel data)
        {
            int resultFinal = 0;
            try
            {
                DistanciaFh DistanciaFH = new DistanciaFh();

                DistanciaFH.CódigoOrigem = data.Origem;
                DistanciaFH.CódigoDestino = data.Destino;
                DistanciaFH.Distância = data.Distancia;
                DistanciaFH.CriadoPor = User.Identity.Name;
                DistanciaFH.DataHoraCriação = DateTime.Now;

                var dbCreateResult = DBDistanciaFh.Create(DistanciaFH);

                if (dbCreateResult == null)
                    resultFinal = 0;
                else
                    resultFinal = 1;
            }
            catch (Exception ex)
            {
                //log
            }
            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteDistanciaFH([FromBody] DistanciaFHViewModel data)
        {
            var result = DBDistanciaFh.Delete(DBDistanciaFh.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateDistanciaFH([FromBody] List<DistanciaFHViewModel> data)
        {
            List<DistanciaFh> results = DBDistanciaFh.GetAll();

            data.RemoveAll(x => DBDistanciaFh.ParseListToViewModel(results).Any(
                u =>
                    u.Origem == x.Origem &&
                    u.Destino == x.Destino &&
                    u.Distancia == x.Distancia &&
                    u.CriadoPor == x.CriadoPor &&
                    u.DataHoraCriacao == x.DataHoraCriacao
            ));

            data.ForEach(x =>
            {
                DistanciaFh toUpdate = DBDistanciaFh.ParseToDB(x);
                toUpdate.AlteradoPor = User.Identity.Name;
                DBDistanciaFh.Update(toUpdate);
            });
            return Json(data);
        }

        #endregion

        #region Configuracao Recursos Folha Horas
        public IActionResult ConfiguracaoRecursosFolhaHoras(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetConfiguracaoRecursosFolhaHoras()
        {
            List<TabelaConfRecursosFHViewModel> result = DBTabelaConfRecursosFh.ParseListToViewModel(DBTabelaConfRecursosFh.GetAll());

            if (result != null)
            {
                result.ForEach(x =>
                {
                    x.Descricao = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.CodigoRecurso, "", 0, "").FirstOrDefault().Name;
                    x.UnidMedida = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.CodigoRecurso, "", 0, "").FirstOrDefault().MeasureUnit;
                });
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateConfiguracaoRecursosFolhaHoras([FromBody] TabelaConfRecursosFHViewModel data)
        {
            int resultFinal = 0;

            TabelaConfRecursosFh toCreate = DBTabelaConfRecursosFh.ParseToDB(data);
            //toCreate.UtilizadorCriacao = User.Identity.Name;
            var result = DBTabelaConfRecursosFh.Create(toCreate);

            if (result == null)
                resultFinal = 0;
            else
                resultFinal = 1;

            return Json(resultFinal);
        }

        [HttpPost]
        public JsonResult DeleteConfiguracaoRecursosFolhaHoras([FromBody] TabelaConfRecursosFHViewModel data)
        {
            var result = DBTabelaConfRecursosFh.Delete(DBTabelaConfRecursosFh.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateConfiguracaoRecursosFolhaHoras([FromBody] List<TabelaConfRecursosFHViewModel> data)
        {
            List<TabelaConfRecursosFh> results = DBTabelaConfRecursosFh.GetAll();

            data.RemoveAll(x => DBTabelaConfRecursosFh.ParseListToViewModel(results).Any(
                u =>
                    u.Tipo == x.Tipo &&
                    u.CodigoRecurso == x.CodigoRecurso &&
                    u.Descricao == x.Descricao &&
                    u.UnidMedida == x.UnidMedida &&
                    u.PrecoUnitarioCusto == x.PrecoUnitarioCusto &&
                    u.PrecoUnitarioVenda == x.PrecoUnitarioVenda &&
                    u.RubricaSalarial == x.RubricaSalarial
            ));

            data.ForEach(x =>
            {
                TabelaConfRecursosFh toUpdate = DBTabelaConfRecursosFh.ParseToDB(x);
                //toUpdate.UtilizadorModificacao = User.Identity.Name;
                toUpdate.Descricao = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.CodigoRecurso, "", 0, "").FirstOrDefault().Name;
                toUpdate.UnidMedida = DBNAV2017Resources.GetAllResources(_config.NAVDatabaseName, _config.NAVCompanyName, x.CodigoRecurso, "", 0, "").FirstOrDefault().MeasureUnit;
                DBTabelaConfRecursosFh.Update(toUpdate);
            });
            return Json(data);
        }
        #endregion

        #region Tipo Requisição
        public IActionResult TiposRequisicao(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetRequisitionTypes()
        {
            List<RequesitionTypeViewModel> result = DBRequesitionType.GetAll(_config.NAVDatabaseName, _config.NAVCompanyName).Select(x => new RequesitionTypeViewModel()
            {
                Code = x.Code,
                Description = x.Description
            }).ToList();
            return Json(result);
        }

        //[HttpPost]
        //public JsonResult UpdateRequesitionTypes([FromBody] List<RequesitionTypeViewModel> data)
        //{
        //    List<NAVRequisitionTypeViewModel> results = DBRequesitionType.GetAll(_config.NAVDatabaseName, _config.NAVCompanyName);
        //    results.RemoveAll(x => data.Any(u => u.Code == x.Code));
        //    results.ForEach(x => DBRequesitionType.Delete(x));
        //    data.ForEach(x =>
        //    {
        //        TiposRequisições TR = new TiposRequisições()
        //        {
        //            Descrição = x.Description
        //        };
        //        if (x.Code > 0)
        //        {
        //            TR.Código = x.Code;
        //            TR.Frota = x.Fleet;
        //            TR.DataHoraModificação = DateTime.Now;
        //            TR.UtilizadorModificação = User.Identity.Name;
        //            DBRequesitionType.Update(TR);
        //        }
        //        else
        //        {
        //            TR.DataHoraCriação = DateTime.Now;
        //            TR.UtilizadorCriação = User.Identity.Name;
        //            TR.Frota = x.Fleet;
        //            DBRequesitionType.Create(TR);
        //        }
        //    });
        //    return Json(data);
        //}
        #endregion

        #region Configurações Aprovações

        public IActionResult ConfiguracaoAprovacoes(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetApprovalConfig()
        {
            List<ApprovalConfigurationsViewModel> result = DBApprovalConfigurations.ParseToViewModel(DBApprovalConfigurations.GetAll());
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeteleApprovalConfig([FromBody] ApprovalConfigurationsViewModel data)
        {
            var result = DBApprovalConfigurations.Delete(DBApprovalConfigurations.ParseToDatabase(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateApprovalConfig([FromBody] List<ApprovalConfigurationsViewModel> data)
        {

            data.ForEach(x =>
            {
                ConfiguraçãoAprovações aprovConfig = new ConfiguraçãoAprovações()
                {
                    Tipo = x.Type,
                    NívelAprovação = x.Level,
                    ValorAprovação = x.ApprovalValue,
                    GrupoAprovação = x.ApprovalGroup,
                    UtilizadorAprovação = x.ApprovalUser,
                    Área = x.Area,
                    CódigoÁreaFuncional = x.FunctionalArea,
                    CódigoCentroResponsabilidade = x.ResponsabilityCenter,
                    CódigoRegião = x.Region,
                    DataInicial = string.IsNullOrEmpty(x.StartDate) ? (DateTime?)null : DateTime.Parse(x.StartDate),
                    DataFinal = string.IsNullOrEmpty(x.EndDate) ? (DateTime?)null : DateTime.Parse(x.EndDate)
                };

                if (!aprovConfig.NívelAprovação.HasValue || aprovConfig.NívelAprovação.Value <= 0)
                    throw new Exception("O nível de aprovação tem que ser maior que zero.");

                if (x.Id > 0)
                {
                    aprovConfig.Id = x.Id;
                    aprovConfig.UtilizadorCriação = x.CreateUser;
                    aprovConfig.DataHoraCriação = x.CreateDate;
                    aprovConfig.CódigoÁreaFuncional = x.FunctionalArea;
                    aprovConfig.CódigoCentroResponsabilidade = x.ResponsabilityCenter;
                    aprovConfig.CódigoRegião = x.Region;
                    aprovConfig.DataHoraModificação = DateTime.Now;
                    aprovConfig.UtilizadorModificação = User.Identity.Name;
                    DBApprovalConfigurations.Update(aprovConfig);
                }
                else
                {
                    aprovConfig.DataHoraCriação = DateTime.Now;
                    aprovConfig.UtilizadorCriação = User.Identity.Name;
                    DBApprovalConfigurations.Create(aprovConfig);
                }
            });
            return Json(data);
        }
        #endregion

        #region Grupo Aprovações
        public IActionResult GruposAprovacoes(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        [HttpPost]
        public JsonResult GetApprovalGroup()
        {
            List<ApprovalGroupViewModel> result = DBApprovalGroups.ParseToViewModel(DBApprovalGroups.GetAll());
            return Json(result);
        }

        public JsonResult GetApprovalGroupID([FromBody] int id)
        {
            ApprovalGroupViewModel result = DBApprovalGroups.ParseToViewModel(DBApprovalGroups.GetById(id));
            return Json(result);
        }

        public JsonResult CreateApprovalGroup([FromBody] ApprovalGroupViewModel data)
        {
            ApprovalGroupViewModel result;
            //Create new 
            result = DBApprovalGroups.ParseToViewModel(DBApprovalGroups.Create(DBApprovalGroups.ParseToDatabase(data)));
            if (result != null)
            {
                result.eReasonCode = 100;
            }
            else
                result.eReasonCode = 101;
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdateApprovalGroup([FromBody] ApprovalGroupViewModel item)
        {
            DBApprovalGroups.Update(DBApprovalGroups.ParseToDatabase(item));

            return Json(item);
        }

        public JsonResult DeleteApprovalGroup([FromBody] ApprovalGroupViewModel data)
        {
            string eReasonCode = "";
            if (!DBApprovalConfigurations.GetAll().Exists(x => x.GrupoAprovação == data.Code) && !DBApprovalConfigurations.GetAll().Exists(x => x.UtilizadorAprovação == data.Description))
            {
                List<UtilizadoresGruposAprovação> results2 = DBApprovalUserGroup.GetAll();
                results2.ForEach(x =>
                {
                    if (x.GrupoAprovação == data.Code)
                        DBApprovalUserGroup.Delete(x);
                });

                List<GruposAprovação> results = DBApprovalGroups.GetAll();
                results.ForEach(x =>
                {
                    if (x.Código == data.Code)
                        DBApprovalGroups.Delete(x);
                });
                eReasonCode = "100";
            }
            else
            {
                eReasonCode = "101";
            }
            return Json(eReasonCode);
        }

        #endregion

        #region Detalhes Grupos Aprovacoes

        public IActionResult DetalhesGruposAprovacoes(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.GroupApproval = "";
                ViewBag.IDGroupApproval = "";
                if (id != null)
                {
                    int IDGroup = Int32.Parse(id);
                    ViewBag.IDGroupApproval = IDGroup;
                }
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;

                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }


        public JsonResult GetDetailsApprovalGroup([FromBody] int id)
        {

            List<ApprovalUserGroupViewModel> result = DBApprovalUserGroup.ParseToViewModel(DBApprovalUserGroup.GetByGroup(id));
            return Json(result);

        }

        public JsonResult CreateDetailsApprovalGroup([FromBody] ApprovalUserGroupViewModel data)
        {
            string eReasonCode = "";
            //Create new 
            eReasonCode = DBApprovalUserGroup.Create(DBApprovalUserGroup.ParseToDb(data)) == null ? "101" : "";

            if (String.IsNullOrEmpty(eReasonCode))
            {
                return Json(data);
            }
            else
            {
                return Json(eReasonCode);
            }

        }

        [HttpPost]
        public JsonResult DeteleDetailsApprovalGroup([FromBody] ApprovalUserGroupViewModel data)
        {
            var result = DBApprovalUserGroup.Delete(DBApprovalUserGroup.ParseToDb(data));

            return Json(data);
        }
        #endregion

        #region Locais
        public IActionResult Locais(string id)
        {
            UserAccessesViewModel UPerm = GetPermissions(id);
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetPlace()
        {
            List<PlacesViewModel> result = DBPlaces.GetAll().Select(x => new PlacesViewModel()
            {
                Code = x.Código,
                Description = x.Descrição,
                Address = x.Endereço,
                Locality = x.Localidade,
                Postalcode = x.CódigoPostal,
                Contact = x.Contacto,
                Responsiblerecept = x.ResponsávelReceção,
                CreateDate = x.DataHoraCriação.HasValue ? x.DataHoraCriação.Value.ToString("yyyy-MM-dd hh:mm:ss.ff") : "",
                CreateUser = x.UtilizadorCriação
            }).ToList();
            return Json(result);
        }
        [HttpPost]
        public JsonResult DeletePlace([FromBody] PlacesViewModel data)
        {
            var result = DBPlaces.Delete(DBPlaces.ParseToDB(data));
            return Json(result);
        }

        [HttpPost]
        public JsonResult UpdatePlace([FromBody] List<PlacesViewModel> data)
        {

            data.ForEach(x =>
            {
                Locais localval = new Locais()
                {
                    Descrição = x.Description,
                    CódigoPostal = x.Postalcode,
                    Endereço = x.Address,
                    Localidade = x.Locality,
                    Contacto = x.Contact,
                    ResponsávelReceção = x.Responsiblerecept
                };
                if (x.Code > 0)
                {
                    localval.Código = x.Code;
                    localval.UtilizadorCriação = x.CreateUser;
                    localval.DataHoraCriação = string.IsNullOrEmpty(x.CreateDate) ? (DateTime?)null : DateTime.Parse(x.CreateDate);
                    localval.DataHoraModificação = DateTime.Now;
                    localval.UtilizadorModificação = User.Identity.Name;
                    DBPlaces.Update(localval);
                }
                else
                {
                    localval.DataHoraCriação = DateTime.Now;
                    localval.UtilizadorCriação = User.Identity.Name;
                    DBPlaces.Create(localval);
                }
            });
            return Json(data);
        }
        #endregion

        #region Acordo de Preços

        public IActionResult AcordoPrecos_List()
        {
            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        public IActionResult AcordoPrecos(string id)
        {
            ViewBag.NoProcedimento = id;

            UserAccessesViewModel UPerm = GetPermissions("Administracao");
            if (UPerm != null && UPerm.Read.Value)
            {
                ViewBag.CreatePermissions = !UPerm.Create.Value;
                ViewBag.UpdatePermissions = !UPerm.Update.Value;
                ViewBag.DeletePermissions = !UPerm.Delete.Value;
                return View();
            }
            else
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpPost]
        public JsonResult GetAcordoPrecosConfigData([FromBody] AcordoPrecosModelView data)
        {
            AcordoPrecos AP = DBAcordoPrecos.GetById(data.NoProcedimento);

            AcordoPrecosModelView result = new AcordoPrecosModelView();
     
            if (AP != null)
            {
                result.NoProcedimento = AP.NoProcedimento;
                result.DtInicio = AP.DtInicio;
                result.DtInicioTexto = AP.DtInicio == null ? "" : AP.DtInicio.Value.ToString("yyyy-MM-dd");
                result.DtFim = AP.DtFim;
                result.DtFimTexto = AP.DtFim == null ? "" : AP.DtFim.Value.ToString("yyyy-MM-dd");
                result.ValorTotal = AP.ValorTotal;

                result.FornecedoresAcordoPrecos = DBFornecedoresAcordoPrecos.GetAllByNoProdimento(data.NoProcedimento).Select(x => new FornecedoresAcordoPrecosViewModel()
                {
                    NoProcedimento = x.NoProcedimento,
                    NoFornecedor = x.NoFornecedor,
                    NomeFornecedor = x.NomeFornecedor,
                    Valor = x.Valor,
                    ValorConsumido = x.ValorConsumido
                }).ToList();

                result.LinhasAcordoPrecos = DBLinhasAcordoPrecos.GetAllByNoProcedimento(data.NoProcedimento).Select(x => new LinhasAcordoPrecosViewModel()
                {
                    NoProcedimento = x.NoProcedimento,
                    NoFornecedor = x.NoFornecedor,
                    CodProduto = x.CodProduto,
                    DtValidadeInicio = x.DtValidadeInicio,
                    DtValidadeInicioTexto = x.DtValidadeInicio == null ? "" : Convert.ToDateTime(x.DtValidadeInicio).ToShortDateString(),
                    DtValidadeFim = x.DtValidadeFim,
                    DtValidadeFimTexto = x.DtValidadeFim == null ? "" : Convert.ToDateTime(x.DtValidadeFim).ToShortDateString(),
                    Cresp = x.Cresp,
                    CrespNome = x.Cresp == null ? "" : x.Cresp.ToString() + " - " + DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 3, User.Identity.Name).Where(y => y.Code == x.Cresp).SingleOrDefault().Name,
                    Area = x.Area,
                    AreaNome = x.Area == null ? "" : x.Area.ToString() + " - " + DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 2, User.Identity.Name).Where(y => y.Code == x.Area).SingleOrDefault().Name,
                    Regiao = x.Regiao,
                    RegiaoNome = x.Regiao == null ? "" : x.Regiao.ToString() + " - " + DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 1, User.Identity.Name).Where(y => y.Code == x.Regiao).SingleOrDefault().Name,
                    Localizacao = x.Localizacao,
                    LocalizacaoNome = x.Localizacao == null ? "" : x.Localizacao.ToString() + " - " + DBNAV2017Locations.GetAllLocations(_config.NAVDatabaseName, _config.NAVCompanyName).Where(y => y.Code == x.Localizacao).SingleOrDefault().Name,
                    CustoUnitario = x.CustoUnitario,
                    NomeFornecedor = x.NoFornecedor == null ? "" : x.NoFornecedor.ToString() + " - " + DBNAV2017Vendor.GetVendor(_config.NAVDatabaseName, _config.NAVCompanyName).Where(y => y.No_ == x.NoFornecedor).SingleOrDefault().Name,
                    DescricaoProduto = x.DescricaoProduto,
                    Um = x.Um,
                    QtdPorUm = x.QtdPorUm,
                    PesoUnitario = x.PesoUnitario,
                    CodProdutoFornecedor = x.CodProdutoFornecedor,
                    DescricaoProdFornecedor = x.DescricaoProdFornecedor,
                    FormaEntrega = x.FormaEntrega,
                    FormaEntregaTexto = x.FormaEntrega == null ? "" : EnumerablesFixed.AP_FormaEntrega.Where(y => y.Id == x.FormaEntrega).SingleOrDefault().Value,
                    UserId = x.UserId,
                    DataCriacao = x.DataCriacao,
                    DataCriacaoTexto = x.DataCriacao == null ? "" : Convert.ToDateTime(x.DataCriacao).ToShortDateString(),
                    TipoPreco = x.TipoPreco,
                    TipoPrecoTexto = x.TipoPreco == null ? "" : EnumerablesFixed.AP_TipoPreco.Where(y => y.Id == x.TipoPreco).SingleOrDefault().Value
                }).ToList();

                //ORIGEM = 1 » Acordo Preços
                //TIPO = 2 » ERRO
                result.AnexosErros = DBAnexosErros.GetByOrigemAndCodigo(1, data.NoProcedimento).Select(x => new AnexosErrosViewModel()
                {
                    ID = x.Id,
                    CodeTexto = x.Id.ToString(),
                    Origem = (int)x.Origem,
                    OrigemTexto = x.Origem == 0 ? "" : EnumerablesFixed.AE_Origem.Where(y => y.Id == x.Origem).SingleOrDefault().Value,
                    Tipo = (int)x.Tipo,
                    TipoTexto = x.Tipo == 0 ? "" : EnumerablesFixed.AE_Tipo.Where(y => y.Id == x.Tipo).SingleOrDefault().Value,
                    Codigo = x.Codigo,
                    NomeAnexo = x.NomeAnexo,
                    Anexo = x.Anexo,
                    CriadoPor = x.CriadoPor,
                    CriadoPorNome = x.CriadoPor == null ? "" : DBUserConfigurations.GetById(x.CriadoPor).Nome,
                    DataHora_Criacao = x.DataHoraCriacao,
                    DataHora_CriacaoTexto = x.DataHoraCriacao == null ? "" : x.DataHoraCriacao.Value.ToString("yyyy-MM-dd"),
                    AlteradoPor = x.AlteradoPor,
                    AlteradoPorNome = x.AlteradoPor == null ? "" : DBUserConfigurations.GetById(x.AlteradoPor).Nome,
                    DataHora_Alteracao = x.DataHoraAlteracao,
                    DataHora_AlteracaoTexto = x.DataHoraAlteracao == null ? "" : x.DataHoraAlteracao.Value.ToString("yyyy-MM-dd")
                }).ToList();
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult GetListAcordoPrecos()
        {
            List<AcordoPrecosModelView> result = DBAcordoPrecos.GetAll();

            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateAcordoPrecos([FromBody] AcordoPrecos data)
        {
            AcordoPrecos toCreate = DBAcordoPrecos.Create(new AcordoPrecos()
            {
                NoProcedimento = data.NoProcedimento,
                DtInicio = data.DtInicio,
                DtFim = data.DtFim,
                ValorTotal = data.ValorTotal
            });

            if (toCreate != null)
                return Json(0);
            else
                return Json(1);
        }

        [HttpPost]
        public JsonResult DeleteAcordoPreco([FromBody] AcordoPrecosModelView data)
        {
            int result = 0;
            bool dbDeleteLinhaResult = false;
            bool dbDeleteFornecedorResult = false;
            bool dbDeleteAcordoPrecoResult = false;

            try
            {
                dbDeleteLinhaResult = DBLinhasAcordoPrecos.DeleteByProcedimento(data.NoProcedimento);

                dbDeleteFornecedorResult = DBFornecedoresAcordoPrecos.DeleteByProcedimento(data.NoProcedimento);

                dbDeleteAcordoPrecoResult = DBAcordoPrecos.Delete(data.NoProcedimento);

                if (!dbDeleteAcordoPrecoResult)
                    result = 1;
            }
            catch (Exception ex)
            {
                result = 99;
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteLinha([FromBody] LinhasAcordoPrecosViewModel data)
        {
            int result = 0;
            bool dbDeleteLinhaResult = false;

            try
            {
                dbDeleteLinhaResult = DBLinhasAcordoPrecos.Delete(data.NoProcedimento, data.NoFornecedor, data.CodProduto, data.DtValidadeInicio, data.Cresp, data.Localizacao);

                if (!dbDeleteLinhaResult)
                    result = 1;
            }
            catch (Exception ex)
            {
                result = 99;
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteFornecedor([FromBody] FornecedoresAcordoPrecosViewModel data)
        {
            int result = 0;
            bool dbDeleteFornecedorResult = false;

            try
            {
                dbDeleteFornecedorResult = DBFornecedoresAcordoPrecos.Delete(data.NoProcedimento, data.NoFornecedor);

                if (!dbDeleteFornecedorResult)
                    result = 1;
            }
            catch (Exception ex)
            {
                result = 99;
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult CreateLinhaAcordoPrecos([FromBody] LinhasAcordoPrecos data)
        {
            LinhasAcordoPrecos toCreate = DBLinhasAcordoPrecos.Create(new LinhasAcordoPrecos()
            {
                NoProcedimento = data.NoProcedimento,
                NoFornecedor = data.NoFornecedor,
                CodProduto = data.CodProduto,
                DtValidadeInicio = data.DtValidadeInicio,
                DtValidadeFim = data.DtValidadeFim,
                Cresp = data.Cresp,
                Area = data.Area,
                Regiao = data.Regiao,
                Localizacao = data.Localizacao,
                CustoUnitario = data.CustoUnitario,
                NomeFornecedor = DBNAV2017Vendor.GetVendor(_config.NAVDatabaseName, _config.NAVCompanyName).Where(x => x.No_ == data.NoFornecedor).SingleOrDefault().Name,
                DescricaoProduto = DBNAV2017Products.GetAllProducts(_config.NAVDatabaseName, _config.NAVCompanyName, data.CodProduto).SingleOrDefault().Name,
                Um = data.Um,
                QtdPorUm = data.QtdPorUm,
                PesoUnitario = data.PesoUnitario,
                CodProdutoFornecedor = data.CodProdutoFornecedor,
                DescricaoProdFornecedor = "",
                FormaEntrega = data.FormaEntrega,
                UserId = User.Identity.Name,
                DataCriacao = DateTime.Now,
                TipoPreco = data.TipoPreco
            });

            if (toCreate != null)
                return Json(0);
            else
                return Json(1);
        }

        [HttpPost]
        public JsonResult CreateFornecedorAcordoPrecos([FromBody] FornecedoresAcordoPrecos data)
        {
            FornecedoresAcordoPrecos toCreate = DBFornecedoresAcordoPrecos.Create(new FornecedoresAcordoPrecos()
            {
                NoProcedimento = data.NoProcedimento,
                NoFornecedor = data.NoFornecedor,
                NomeFornecedor = DBNAV2017Vendor.GetVendor(_config.NAVDatabaseName, _config.NAVCompanyName).Where(x => x.No_ == data.NoFornecedor).SingleOrDefault().Name,
                Valor = data.Valor,
                ValorConsumido = data.ValorConsumido
            });

            if (toCreate != null)
                return Json(0);
            else
                return Json(1);
        }

        [HttpPost]
        public JsonResult VerificarNoProcedimento([FromBody] AcordoPrecos data)
        {
            AcordoPrecos AcordoPrecos =  DBAcordoPrecos.GetById(data.NoProcedimento);

            if (AcordoPrecos == null)
                return Json(0);
            else
                return Json(1);
        }

        [HttpPost]
        [Route("Administracao/FileUpload")]
        [Route("Administracao/FileUpload/{FormularioNoProcedimento}")]
        public JsonResult FileUpload(string FormularioNoProcedimento)
        {
            //TESTE COM DLL EPPlus
            var files = Request.Form.Files;
            bool global_result = true;
            foreach (var file in files)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(file.FileName);
                    string filename = Path.GetFileName(file.FileName);
                    //LOCAL TEST
                    //string full_path = "C:\\Users\\ARomao\\Desktop\\" + filename;
                    //WEB TEST
                    var full_path = Path.Combine(_generalConfig.FileUploadFolder, User.Identity.Name + "_" + filename);
                    if (System.IO.File.Exists(full_path))
                        System.IO.File.Delete(full_path);
                    FileStream dd = new FileStream(full_path, FileMode.CreateNew);
                    file.CopyTo(dd);
                    dd.Dispose();
                    var existingFile = new FileInfo(full_path);

                    string filename_result = name + "_Resultado.xlsx";
                    //LOCAL TEST
                    //string full_path_result = "C:\\Users\\ARomao\\Desktop\\" + "AcordoPrecos_Result.xlsx";
                    //WEB TEST
                    var full_path_result = Path.Combine(_generalConfig.FileUploadFolder, User.Identity.Name + "_" + filename_result);
                    if (System.IO.File.Exists(full_path_result))
                        System.IO.File.Delete(full_path_result);
                    var existingFile_result = new FileInfo(full_path_result);

                    using (var excel = new ExcelPackage(existingFile))
                    {
                        var excel_result = new ExcelPackage(existingFile_result);
                        ExcelWorkbook workBook_result = excel_result.Workbook;

                        ExcelWorkbook workBook = excel.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0 && workBook.Worksheets[0].Name == "LINHAS")
                            {
                                workBook_result = Criar_Excel_Worksheet(workBook_result, "SUCESSO");
                                workBook_result = Criar_Excel_Worksheet(workBook_result, "ERRO");

                                ExcelWorksheet currentWorksheet = workBook.Worksheets["LINHAS"];
                                ExcelWorksheet currentWorksheet_SUCESSO = workBook_result.Worksheets["SUCESSO"];
                                ExcelWorksheet currentWorksheet_ERRO = workBook_result.Worksheets["ERRO"];

                                if ((currentWorksheet.Dimension.End.Row > 1 && currentWorksheet.Dimension.End.Column == 16) &&
                                    (currentWorksheet.Cells[1, 1].Value.ToString() == "NoProcedimento") &&
                                    (currentWorksheet.Cells[1, 2].Value.ToString() == "NoFornecedor") &&
                                    (currentWorksheet.Cells[1, 3].Value.ToString() == "CodProduto") &&
                                    (currentWorksheet.Cells[1, 4].Value.ToString() == "DtValidadeInicio") &&
                                    (currentWorksheet.Cells[1, 5].Value.ToString() == "DtValidadeFim") &&
                                    (currentWorksheet.Cells[1, 6].Value.ToString() == "Regiao") &&
                                    (currentWorksheet.Cells[1, 7].Value.ToString() == "Area") &&
                                    (currentWorksheet.Cells[1, 8].Value.ToString() == "Cresp") &&
                                    (currentWorksheet.Cells[1, 9].Value.ToString() == "Localizacao") &&
                                    (currentWorksheet.Cells[1, 10].Value.ToString() == "CustoUnitario") &&
                                    (currentWorksheet.Cells[1, 11].Value.ToString() == "UM") &&
                                    (currentWorksheet.Cells[1, 12].Value.ToString() == "QtdPorUM") &&
                                    (currentWorksheet.Cells[1, 13].Value.ToString() == "PesoUnitario") &&
                                    (currentWorksheet.Cells[1, 14].Value.ToString() == "CodProdutoFornecedor") &&
                                    (currentWorksheet.Cells[1, 15].Value.ToString() == "FormaEntrega") &&
                                    (currentWorksheet.Cells[1, 16].Value.ToString() == "TipoPreco"))
                                {
                                    int Linha_SUCESSO = 2;
                                    int Linha_ERRO = 2;
                                    var result_list = new List<bool>();
                                    for (int i = 1; i <= 16; i++)
                                    {
                                        result_list.Add(false);
                                    }

                                    string NoProcedimento = "";
                                    string NoFornecedor = "";
                                    string CodProduto = "";
                                    string DtValidadeInicio = "";
                                    string DtValidadeFim = "";
                                    string Regiao = "";
                                    string Area = "";
                                    string Cresp = "";
                                    string Localizacao = "";
                                    string CustoUnitario = "";
                                    string UM = "";
                                    string QtdPorUM = "";
                                    string PesoUnitario = "";
                                    string CodProdutoFornecedor = "";
                                    string FormaEntrega = "";
                                    string TipoPreco = "";

                                    //VALIDAÇÃO DE TODOS OS CAMPOS
                                    for (int rowNumber = 2; rowNumber <= currentWorksheet.Dimension.End.Row; rowNumber++)
                                    {
                                        NoProcedimento = currentWorksheet.Cells[rowNumber, 1].Value.ToString();
                                        NoFornecedor = currentWorksheet.Cells[rowNumber, 2].Value.ToString();
                                        CodProduto = currentWorksheet.Cells[rowNumber, 3].Value.ToString();
                                        DtValidadeInicio = currentWorksheet.Cells[rowNumber, 4].Value.ToString();
                                        DtValidadeFim = currentWorksheet.Cells[rowNumber, 5].Value.ToString();
                                        Regiao = currentWorksheet.Cells[rowNumber, 6].Value.ToString();
                                        Area = currentWorksheet.Cells[rowNumber, 7].Value.ToString();
                                        Cresp = currentWorksheet.Cells[rowNumber, 8].Value.ToString();
                                        Localizacao = currentWorksheet.Cells[rowNumber, 9].Value.ToString();
                                        CustoUnitario = currentWorksheet.Cells[rowNumber, 10].Value.ToString();
                                        UM = currentWorksheet.Cells[rowNumber, 11].Value.ToString();
                                        QtdPorUM = currentWorksheet.Cells[rowNumber, 12].Value.ToString();
                                        PesoUnitario = currentWorksheet.Cells[rowNumber, 13].Value.ToString();
                                        CodProdutoFornecedor = currentWorksheet.Cells[rowNumber, 14].Value.ToString();
                                        FormaEntrega = currentWorksheet.Cells[rowNumber, 15].Value.ToString();
                                        TipoPreco = currentWorksheet.Cells[rowNumber, 16].Value.ToString();

                                        result_list = Validar_LinhaExcel(FormularioNoProcedimento, NoProcedimento, NoFornecedor, CodProduto, DtValidadeInicio, DtValidadeFim, Regiao, Area, Cresp, Localizacao, CustoUnitario, QtdPorUM, PesoUnitario, FormaEntrega, TipoPreco, result_list);

                                        if (result_list.All(c => c == false))
                                        {
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 1].Value = NoProcedimento;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 2].Value = NoFornecedor;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 3].Value = CodProduto;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 4].Value = DtValidadeInicio;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 5].Value = DtValidadeFim;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 6].Value = Regiao;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 7].Value = Area;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 8].Value = Cresp;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 9].Value = Localizacao;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 10].Value = CustoUnitario;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 11].Value = UM;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 12].Value = QtdPorUM;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 13].Value = PesoUnitario;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 14].Value = CodProdutoFornecedor;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 15].Value = FormaEntrega;
                                            currentWorksheet_SUCESSO.Cells[Linha_SUCESSO, 16].Value = TipoPreco;

                                            Linha_SUCESSO = Linha_SUCESSO + 1;
                                        }
                                        else
                                        {
                                            global_result = false;

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 1].Value = NoProcedimento;
                                            if (result_list[1] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 1].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 2].Value = NoFornecedor;
                                            if (result_list[2] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 2].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 3].Value = CodProduto;
                                            if (result_list[3] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 3].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 4].Value = DtValidadeInicio;
                                            if (result_list[4] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 4].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 5].Value = DtValidadeFim;
                                            if (result_list[5] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 5].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 6].Value = Regiao;
                                            if (result_list[6] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 6].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 7].Value = Area;
                                            if (result_list[7] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 7].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 8].Value = Cresp;
                                            if (result_list[8] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 8].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 9].Value = Localizacao;
                                            if (result_list[9] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 9].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 10].Value = CustoUnitario;
                                            if (result_list[10] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 10].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 11].Value = UM;

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 12].Value = QtdPorUM;
                                            if (result_list[11] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 12].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 13].Value = PesoUnitario;
                                            if (result_list[12] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 13].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 14].Value = CodProdutoFornecedor;

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 15].Value = FormaEntrega;
                                            if (result_list[13] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 15].Style.Font.Color.SetColor(Color.Red);

                                            currentWorksheet_ERRO.Cells[Linha_ERRO, 16].Value = TipoPreco;
                                            if (result_list[14] == true)
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 16].Style.Font.Color.SetColor(Color.Red);

                                            if (result_list[15] == true)
                                            {
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 1].Style.Font.Color.SetColor(Color.Orange);
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 2].Style.Font.Color.SetColor(Color.Orange);
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 3].Style.Font.Color.SetColor(Color.Orange);
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 4].Style.Font.Color.SetColor(Color.Orange);
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 8].Style.Font.Color.SetColor(Color.Orange);
                                                currentWorksheet_ERRO.Cells[Linha_ERRO, 9].Style.Font.Color.SetColor(Color.Orange);
                                            }

                                            Linha_ERRO = Linha_ERRO + 1;
                                        }

                                        if (result_list.All(c => c == false))
                                        {
                                            LinhasAcordoPrecos toCreate = DBLinhasAcordoPrecos.Create(new LinhasAcordoPrecos()
                                            {
                                                NoProcedimento = NoProcedimento,
                                                NoFornecedor = NoFornecedor,
                                                CodProduto = CodProduto,
                                                DtValidadeInicio = Convert.ToDateTime(DtValidadeInicio),
                                                DtValidadeFim = Convert.ToDateTime(DtValidadeFim),
                                                Cresp = Cresp,
                                                Area = Area,
                                                Regiao = Regiao,
                                                Localizacao = Localizacao,
                                                CustoUnitario = Convert.ToDecimal(CustoUnitario),
                                                NomeFornecedor = DBNAV2017Vendor.GetVendor(_config.NAVDatabaseName, _config.NAVCompanyName).Where(x => x.No_ == NoFornecedor).SingleOrDefault().Name,
                                                DescricaoProduto = DBNAV2017Products.GetAllProducts(_config.NAVDatabaseName, _config.NAVCompanyName, CodProduto).SingleOrDefault().Name,
                                                Um = UM,
                                                QtdPorUm = Convert.ToDecimal(QtdPorUM),
                                                PesoUnitario = Convert.ToDecimal(PesoUnitario),
                                                CodProdutoFornecedor = CodProdutoFornecedor,
                                                DescricaoProdFornecedor = "",
                                                FormaEntrega = Convert.ToInt32(FormaEntrega),
                                                UserId = User.Identity.Name,
                                                DataCriacao = DateTime.Now,
                                                TipoPreco = Convert.ToInt32(TipoPreco)
                                            });
                                        }
                                    }

                                    excel_result.Save();

                                    byte[] Anexo_Result = System.IO.File.ReadAllBytes(full_path_result);

                                    AnexosErros newAnexo = new AnexosErros();
                                    newAnexo.Origem = 1; //ACORDO DE PREÇOS
                                    if (global_result)
                                        newAnexo.Tipo = 1; //SUCESSO
                                    else
                                        newAnexo.Tipo = 2; //INSUCESSO
                                    newAnexo.Codigo = FormularioNoProcedimento;
                                    newAnexo.NomeAnexo = filename_result;
                                    newAnexo.Anexo = Anexo_Result;
                                    newAnexo.CriadoPor = User.Identity.Name;
                                    newAnexo.DataHoraCriacao = DateTime.Now;
                                    DBAnexosErros.Create(newAnexo);

                                    excel.Dispose();
                                    excel_result.Dispose();

                                    System.IO.File.Delete(full_path_result);
                                    System.IO.File.Delete(full_path);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return Json("");
        }

        public List<bool> Validar_LinhaExcel(string FormularioNoProcedimento, string NoProcedimento, string NoFornecedor, string CodProduto, string DtValidadeInicio, string DtValidadeFim,
            string Regiao, string Area, string Cresp, string Localizacao, string CustoUnitario, string QtdPorUM, string PesoUnitario,
            string FormaEntrega, string TipoPreco, List<bool> result_list)
        {
            DateTime currectDate;
            decimal currectDecimal;
            int currectInt;

            for (int i = 1; i <= 15; i++)
            {
                result_list[i] = false;
            }

            if (DBAcordoPrecos.GetAll().Where(x => x.NoProcedimento == NoProcedimento).Count() == 0 || FormularioNoProcedimento != NoProcedimento)
                result_list[1] = true;

            if (DBNAV2017Vendor.GetVendor(_config.NAVDatabaseName, _config.NAVCompanyName).Where(x => x.No_ == NoFornecedor).Count() == 0)
                result_list[2] = true;

            if (DBNAV2017Products.GetAllProducts(_config.NAVDatabaseName, _config.NAVCompanyName, CodProduto).Count() == 0)
                result_list[3] = true;

            if (!DateTime.TryParse(DtValidadeInicio, out currectDate))
                result_list[4] = true;

            if (!DateTime.TryParse(DtValidadeFim, out currectDate))
                result_list[5] = true;

            if (DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 1, User.Identity.Name).Where(x => x.Code == Regiao).Count() == 0)
                result_list[6] = true;

            if (DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 2, User.Identity.Name).Where(x => x.Code == Area).Count() == 0)
                result_list[7] = true;

            if (DBNAV2017DimensionValues.GetByDimTypeAndUserId(_config.NAVDatabaseName, _config.NAVCompanyName, 3, User.Identity.Name).Where(x => x.Code == Cresp).Count() == 0)
                result_list[8] = true;

            if (DBAcessosLocalizacoes.GetByUserId(User.Identity.Name).Where(x => x.Localizacao == Localizacao).Count() == 0)
                result_list[9] = true;

            if (!decimal.TryParse(CustoUnitario, out currectDecimal))
                result_list[10] = true;

            if (!decimal.TryParse(QtdPorUM, out currectDecimal))
                result_list[11] = true;

            if (!decimal.TryParse(PesoUnitario, out currectDecimal))
                result_list[12] = true;

            if (int.TryParse(FormaEntrega, out currectInt))
            {
                if (EnumerablesFixed.AP_FormaEntrega.Where(x => x.Id == Convert.ToInt32(FormaEntrega)).Count() == 0)
                    result_list[13] = true;
            }
            else
                result_list[13] = true;

            if (int.TryParse(TipoPreco, out currectInt))
            {
                if (EnumerablesFixed.AP_TipoPreco.Where(x => x.Id == Convert.ToInt32(TipoPreco)).Count() == 0)
                    result_list[14] = true;
            }
            else
                result_list[14] = true;

            if (DBLinhasAcordoPrecos.GetAll().Where(x => x.NoProcedimento == NoProcedimento && x.NoFornecedor == NoFornecedor && x.CodProduto == CodProduto &&
                    x.DtValidadeInicio == Convert.ToDateTime(DtValidadeInicio) && x.Cresp == Cresp && x.Localizacao == Localizacao).Count() > 0)
                result_list[15] = true;


            return result_list;
        }

        public ExcelWorkbook Criar_Excel_Worksheet(ExcelWorkbook workBook, string Nome)
        {
            workBook.Worksheets.Add(Nome);
            ExcelWorksheet currentWorksheet = workBook.Worksheets[Nome];

            currentWorksheet.Cells[1, 1].Value = "NoProcedimento";
            currentWorksheet.Cells[1, 2].Value = "NoFornecedor";
            currentWorksheet.Cells[1, 3].Value = "CodProduto";
            currentWorksheet.Cells[1, 4].Value = "DtValidadeInicio";
            currentWorksheet.Cells[1, 5].Value = "DtValidadeFim";
            currentWorksheet.Cells[1, 6].Value = "Regiao";
            currentWorksheet.Cells[1, 7].Value = "Area";
            currentWorksheet.Cells[1, 8].Value = "Cresp";
            currentWorksheet.Cells[1, 9].Value = "Localizacao";
            currentWorksheet.Cells[1, 10].Value = "CustoUnitario";
            currentWorksheet.Cells[1, 11].Value = "UM";
            currentWorksheet.Cells[1, 12].Value = "QtdPorUM";
            currentWorksheet.Cells[1, 13].Value = "PesoUnitario";
            currentWorksheet.Cells[1, 14].Value = "CodProdutoFornecedor";
            currentWorksheet.Cells[1, 15].Value = "FormaEntrega";
            currentWorksheet.Cells[1, 16].Value = "TipoPreco";

            return workBook;
        }

        [HttpGet]
        public FileResult DownloadFileAnexosErros(string iD)
        {
            AnexosErros AnexoErro = DBAnexosErros.GetById(Convert.ToInt32(iD));

            return File(AnexoErro.Anexo, System.Net.Mime.MediaTypeNames.Application.Octet, AnexoErro.NomeAnexo);
        }

        [HttpGet]
        [Route("Administracao/DownloadAcordoPrecosTemplate")]
        [Route("Administracao/DownloadAcordoPrecosTemplate/{FileName}")]
        public FileStreamResult DownloadAcordoPrecosTemplate(string FileName)
        {
            return new FileStreamResult(new FileStream(_generalConfig.FileUploadFolder + FileName, FileMode.Open), "application /xlsx");
        }
        

        [HttpPost]
        public JsonResult DeleteAnexosErros([FromBody] AnexosErrosViewModel AnexoErro)
        {
            int result = 0;
            bool dbDeleteAnexosErrosResult = false;

            try
            {
                dbDeleteAnexosErrosResult = DBAnexosErros.Delete(Convert.ToInt32(AnexoErro.CodeTexto));

                if (!dbDeleteAnexosErrosResult)
                    result = 1;
            }
            catch (Exception ex)
            {
                result = 99;
            }
            return Json(result);
        }
        
        #endregion Acordo de Preços


        #endregion

        public UserAccessesViewModel GetPermissions(string id)
        {
            UserAccessesViewModel UPerm = new UserAccessesViewModel();
            if (id == "Engenharia")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Engenharia, Enumerations.Features.Administração);
            }
            if (id == "Ambiente")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Ambiente, Enumerations.Features.Administração);
            }
            if (id == "Nutricao")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Nutrição, Enumerations.Features.Administração);
            }
            if (id == "Vendas")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Vendas, Enumerations.Features.Administração);
            }
            if (id == "Apoio")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Apoio, Enumerations.Features.Administração);
            }
            if (id == "PO")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.PO, Enumerations.Features.Administração);
            }
            if (id == "NovasAreas")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.NovasÁreas, Enumerations.Features.Administração);
            }
            if (id == "Internacionalizacao")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Internacional, Enumerations.Features.Administração);
            }
            if (id == "Juridico")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Jurídico, Enumerations.Features.Administração);
            }
            if (id == "Compras")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Compras, Enumerations.Features.Administração);
            }
            if (id == "Administracao")
            {
                UPerm = DBUserAccesses.GetByUserAreaFunctionality(User.Identity.Name, Enumerations.Areas.Administração, Enumerations.Features.Administração);
            }

            return UPerm;
        }
    }
}