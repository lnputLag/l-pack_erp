namespace Client.Interfaces.Production
{
    /// <summary>
    /// id мест комплектации
    /// </summary>
    public sealed class ComplectationPlace
    {
        /// <summary>
        /// комплектация на СГП
        /// </summary>
        public const string Stock = "720";

        /// <summary>
        /// комплектация на ГА
        /// </summary>
        public const string CorrugatingMachines = "719";

        /// <summary>
        /// комплектация на переработке
        /// </summary>
        public const string ProcessingMachines = "721";

        /// <summary>
        /// комплектация литой тары
        /// </summary>
        public const string MoldedContainer = "714";

        /// <summary>
        /// комплектация на ГА Кашира
        /// </summary>
        public const string CorrugatingMachinesKsh = "881";

        /// <summary>
        /// комплектация на ПР Кашира
        /// </summary>
        public const string ProcessingMachinesKsh = "882";

        /// <summary>
        /// комплектация на СГП Кашира
        /// </summary>
        public const string StockKsh = "883";
    }
}
