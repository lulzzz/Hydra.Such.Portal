﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Hydra.Such.Data.Database
{
    public partial class FundamentoLegalTipoProcedimentoCcp
    {
        public int IdTipo { get; set; }
        public int IdFundamento { get; set; }
        public string DescricaoFundamento { get; set; }

        public TipoProcedimentoCcp TipoNavigation { get; set; }
        public ICollection<ProcedimentosCcp> Procedimentos { get; set; }
    }
}