﻿using PxStat.Security;

namespace PxStat.Workflow
{
    /// <summary>
    /// DTO for Workflow
    /// </summary>
    internal class Workflow_DTO
    {
        /// <summary>
        /// Release Code
        /// </summary>
        public int RlsCode { get; set; }

        /// <summary>
        /// Current only flag
        /// </summary>
        public bool WrqCurrentFlagOnly { get; set; }

        public string LngIsoCode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameters"></param>
        public Workflow_DTO(dynamic parameters)
        {
            if (parameters.RlsCode != null)
                this.RlsCode = parameters.RlsCode;
            if (parameters.WrqCurrentFlagOnly != null)
                this.WrqCurrentFlagOnly = parameters.WrqCurrentFlagOnly;
            else this.WrqCurrentFlagOnly = true;
            if (parameters.LngIsoCode != null)
                this.LngIsoCode = parameters.LngIsoCode;
            else
                this.LngIsoCode = Configuration_BSO.GetCustomConfig("language.iso.code");

        }

        /// <summary>
        /// Blank constructor
        /// </summary>
        public Workflow_DTO() { }

    }
}
