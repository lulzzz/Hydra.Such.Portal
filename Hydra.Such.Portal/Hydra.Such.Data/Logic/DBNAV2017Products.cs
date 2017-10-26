﻿using Hydra.Such.Data.Database;
using Hydra.Such.Data.ViewModel;
using Hydra.Such.Data.ViewModel.ProjectView;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Hydra.Such.Data.Logic
{
    public class DBNAV2017Products
    {
        public static List<NAVProductsViewModel> GetAllProducts(string NAVDatabaseName, string NAVCompanyName, string productNo)
        {
            try
            {
                List<NAVProductsViewModel> result = new List<NAVProductsViewModel>();
                using (var ctx = new SuchDBContextExtention())
                {
                    var parameters = new[]{
                        new SqlParameter("@DBName", NAVDatabaseName),
                        new SqlParameter("@CompanyName", NAVCompanyName),
                        new SqlParameter("@NoProduto", productNo)
                    };

                    IEnumerable<dynamic> data = ctx.execStoredProcedure("exec NAV2017Produtos @DBName, @CompanyName, @NoProduto", parameters);

                    foreach (dynamic temp in data)
                    {
                        result.Add(new NAVProductsViewModel()
                        {
                            Code = (string)temp.No_,
                            Name = (string)temp.Description,
                            MeasureUnit = (string)temp.Base_Unit_of_Measure
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}