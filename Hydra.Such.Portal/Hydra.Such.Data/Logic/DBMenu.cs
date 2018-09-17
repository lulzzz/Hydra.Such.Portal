﻿using Hydra.Such.Data.Database;
using Hydra.Such.Data.ViewModel.Approvals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Such.Data.Logic
{
    public static class DBMenu
    {
        #region CRUD
        public static Menu GetById(int Id)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Menu.Where(x => x.Id == Id).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public static List<Menu> GetAll()
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    return ctx.Menu.ToList();
                }
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public static Menu Create(Menu ObjectToCreate)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    ctx.Menu.Add(ObjectToCreate);
                    ctx.SaveChanges();
                }

                return ObjectToCreate;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public static Menu Update(Menu ObjectToUpdate)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    ctx.Menu.Update(ObjectToUpdate);
                    ctx.SaveChanges();
                }

                return ObjectToUpdate;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
        #endregion

        public static List<Menu> GetByUserId(string userId)
        {
            try
            {
                using (var ctx = new SuchDBContext())
                {
                    List<Menu> menus = null;
                    HashSet<int> featuresIds = new HashSet<int>();
                    List<int> menusIds = null;

                    // toDo -> get features ids from user id
                    var listProfiles = DBUserProfiles.GetByUserId(userId);
                    if (listProfiles != null && listProfiles.Count() > 0)
                    {
                        var listProfilesId = listProfiles.Select(s => s.IdPerfil).ToList();
                        foreach (var profileId in listProfilesId)
                        {
                            var listAccessProfile = DBAccessProfiles.GetByProfileModelId(profileId).ToList();
                            if (listAccessProfile != null && listAccessProfile.Count() > 0)
                            {
                                var listProfileFeatures = new HashSet<int>();
                                listProfileFeatures = listAccessProfile.Select(s => s.Funcionalidade).ToHashSet<int>();
                                featuresIds.UnionWith(listProfileFeatures);
                            }
                        }
                    }

                    var listUserAccess = DBUserAccesses.GetByUserId(userId);
                    if(listUserAccess != null && listUserAccess.Count() > 0)
                    {
                        var listFeatures = new HashSet<int>();
                        listFeatures = listUserAccess.Select(s => s.Funcionalidade).ToHashSet<int>();
                        featuresIds.UnionWith(listFeatures);
                    }

                    //featuresIds = new List<int> { 1, 2, 3, 4 };
                    // list menu id from features                    
                    if (featuresIds != null && featuresIds.Count() > 0)
                        menusIds = ctx.FeaturesMenus.Where(fm=> featuresIds.Contains(fm.IdFeature)).Select(fm => fm.IdMenu).ToList();
                    // get menu
                    if(menusIds != null && menusIds.Count() > 0)
                        menus = ctx.Menu.Where(m => menusIds.Contains(m.Id)).ToList();

                    return menus;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #region Parses


        #endregion



    }
}
