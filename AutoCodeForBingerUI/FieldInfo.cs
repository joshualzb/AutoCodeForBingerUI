namespace AutoCodeForBingerUI
{
    /// <summary>
    /// 表字段类型
    /// </summary>
    public class FieldInfo
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 是否是主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 是否是自动增长
        /// </summary>
        public bool IsAutomatic { get; set; }

        /// <summary>
        /// 字段长度
        /// </summary>
        public int FieldLength { get; set; }

        /// <summary>
        /// 小数位
        /// </summary>
        public int DecimalPlaces { get; set; }
    }
}
