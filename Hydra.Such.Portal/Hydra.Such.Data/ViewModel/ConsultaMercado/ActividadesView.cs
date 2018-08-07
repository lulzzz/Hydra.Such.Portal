﻿using System;
using System.Collections.Generic;
using System.Text;
using Hydra.Such.Data.Database;

namespace Hydra.Such.Data.ViewModel.ConsultaMercado
{
    public class ActividadesView : ErrorHandler
    {
        public string CodActividade { get; set; }
        public string Descricao { get; set; }

        public ICollection<ActividadesPorFornecedorView> ActividadesPorFornecedor { get; set; }
    }
}
