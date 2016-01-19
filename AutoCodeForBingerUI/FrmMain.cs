using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace AutoCodeForBingerUI
{
    public partial class FrmMain : Form
    {
        System.Configuration.Configuration config = null;

        //应用程序的主入口点。
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }

        public FrmMain()
        {
            InitializeComponent();
        }

        //窗体加载
        private void FrmLogin_Load(object sender, EventArgs e)
        {
            //服务器
            InitConfig("服务器", this.cbxDBServerList);
            //用户名
            InitConfig("用户名", this.textBox1);
            //密码
            InitConfig("密码", this.textBox2);
            //数据库
            InitConfig("数据库", this.cbbDBNames);
            //----------------------------------------------------------------------
            //跟命名空间
            InitConfig("跟命名空间", this.tbBaseNamespace);
            //列表母版页
            InitConfig("列表母版页", this.textBox7);
            //窗口母版页
            InitConfig("窗口母版页", this.textBox8);
            //自动前缀
            InitConfig("自动前缀", this.checkBox1);
            //前缀
            InitConfig("tbPrefix", this.tbPrefix);
            //表名前缀分隔符
            InitConfig("表名前缀分隔符", this.textBox4);
            //编号开始
            InitConfig("编号开始", this.textBox3);
            //编号结束
            InitConfig("编号结束", this.textBox5);
            //排除编号
            InitConfig("排除编号", this.textBox6);
            //----------------------------------------------------------------------
            //生成列表页面
            InitConfig("生成列表页面", this.checkBox5);
            //生成新增页面
            InitConfig("生成新增页面", this.checkBox2);
            //生成修改页面
            InitConfig("生成修改页面", this.checkBox3);
            //生成删除页面
            InitConfig("生成删除页面", this.checkBox4);
        }

        //生成代码
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("请至少在右侧选择一个表！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //文件夹
            string folder = string.Empty;

            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.ShowNewFolderButton = true;//获取或设置一个值，该值指示“新建文件夹”按钮是否显示在文件夹浏览对话框中。

            fbd.SelectedPath = config.AppSettings.Settings["路径"].Value;

            DialogResult dr = fbd.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {

                //根命名空间
                string baseNameSpace = this.tbBaseNamespace.Text;

                this.lblMessage.Text = string.Empty;
                this.toolStripProgressBar1.Visible = true;
                this.toolStripProgressBar1.Size = new System.Drawing.Size(this.Size.Width - 30, this.toolStripProgressBar1.Size.Height);
                this.toolStripProgressBar1.Maximum = this.listView1.SelectedItems.Count;
                this.toolStripProgressBar1.Step = 1;
                this.toolStripProgressBar1.Value = 0;

                this.btnStart.Text = "项目生成中...";
                this.btnStart.Refresh();

                //记录日志
                LogConfig(fbd.SelectedPath);

                SetEnabled(false);

                foreach (ListViewItem item in this.listView1.SelectedItems)
                {
                    //表名
                    string tableName = item.Text;

                    var prefix = GetPrefix(tableName);

                    //当前文件编号 
                    int currentNum = GetFileNumber(prefix);

                    string pk = string.Empty;

                    int height = 50;

                    folder = Path.Combine(fbd.SelectedPath, prefix); //文件夹

                    //表和类型的映射
                    List<FieldInfo> columns = GetColumns(tableName, ref pk, ref height);

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    string className = GetClassName(tableName, prefix);

                    //列表
                    if (this.checkBox5.Checked)
                    {
                        //生成列表页面
                        GenerateListPage(baseNameSpace, prefix, tableName, className, folder, pk, height, columns);
                        GenerateListPageDesigner(baseNameSpace, prefix, className, folder);
                        GenerateListPageCS(baseNameSpace, prefix, className, folder);
                    }
                    //新增 //修改
                    if (this.checkBox2.Checked || this.checkBox3.Checked)
                    {
                        GenerateAddPage(baseNameSpace, prefix, className, folder, pk, columns, height);
                        GenerateAddPageDesigner(baseNameSpace, prefix, className, folder, columns);
                        GenerateAddPageCS(baseNameSpace, prefix, className, folder, pk, columns);
                    }
                    this.toolStripProgressBar1.Value++;
                }
                this.toolStripProgressBar1.Size = new System.Drawing.Size(this.Size.Width - 120, this.toolStripProgressBar1.Size.Height);
                this.btnStart.Text = "一键生成";
                this.lblMessage.Text = "项目生成成功!";
                this.btnStart.Refresh();

                SetEnabled(true);
            }
        }

        //服务器点击
        private void cbxDBServerList_DropDown(object sender, EventArgs e)
        {
            //初始化数据库服务器列表
            FillDBServer();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillTableNames();
        }

        // 刷新服务器
        private void button2_Click_1(object sender, EventArgs e)
        {
            cbxDBServerList.Items.Clear();
            FillDBServer();
        }

        //属性数据库
        private void button3_Click(object sender, EventArgs e)
        {
            this.cbbDBNames.Items.Clear();
            FillDBNames();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FillDBNames();
        }

        //自动前缀
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.tbPrefix.Enabled = !this.checkBox1.Checked;
        }

        #region 私有方法

        private void FillDBServer()
        {
            if (cbxDBServerList.Items.Count == 0)
            {
                SQLDMO.NameList names;

                SQLDMO.ApplicationClass ac = new SQLDMO.ApplicationClass();

                names = ac.ListAvailableSQLServers();

                string[] serverList = new string[names.Count];

                for (int i = 0; i < serverList.Length; i++)
                {
                    serverList[i] = names.Item(i);
                }

                foreach (string severName in serverList)
                {
                    if (severName != null)
                    {
                        cbxDBServerList.Items.Add(severName);
                    }
                }
            }
        }

        //初始化数据库名称
        private void FillDBNames()
        {
            this.button4.Enabled = false;
            this.button4.Text = "登陆中";
            this.button4.Refresh();

            try
            {
                if (this.cbbDBNames.Items.Count == 0)
                {
                    SqlConnection conn = new SqlConnection(string.Format("Data Source={0};User ID={1};Password={2}", this.cbxDBServerList.SelectedItem, this.textBox1.Text, this.textBox2.Text));

                    string sql = "SELECT NAME FROM MASTER..SysDatabases ORDER BY NAME";

                    SqlDataAdapter sda = new SqlDataAdapter(sql, conn);

                    DataSet ds = new DataSet("DBS");

                    conn.Open();

                    this.lblMessage.Text = "登录成功!";

                    sda.Fill(ds);

                    sda.Dispose();

                    conn.Close();

                    var rows = ds.Tables[0].Rows;

                    foreach (DataRow row in rows)
                    {
                        this.cbbDBNames.Items.Add(row[0].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                this.lblMessage.Text = "登录失败!";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.button4.Enabled = true;
                this.button4.Text = "登陆";
            }
        }

        private void FillTableNames()
        {
            try
            {
                SqlConnection conn = new SqlConnection(string.Format("Data Source={0};User ID={1};Password={2};database={3}", this.cbxDBServerList.SelectedItem, this.textBox1.Text, this.textBox2.Text, this.cbbDBNames.SelectedItem));

                string sql = "SELECT T.[NAME] AS TABLENAME, S.[NAME] AS [SCHEMA] FROM SYS.TABLES AS T,SYS.SCHEMAS AS S WHERE T.SCHEMA_ID = S.SCHEMA_ID AND S.[NAME] = 'DBO'  UNION ALL SELECT V.[NAME] AS VIEWNAME, S.[NAME] AS [SCHEMA] FROM SYS.VIEWS AS V,SYS.SCHEMAS AS S WHERE V.SCHEMA_ID = S.SCHEMA_ID AND S.[NAME] = 'DBO'";

                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);

                DataSet ds = new DataSet("DBS");

                conn.Open();

                sda.Fill(ds);

                sda.Dispose();

                conn.Close();

                var rows = ds.Tables[0].Rows;

                this.listView1.Items.Clear();

                foreach (DataRow row in rows)
                {
                    this.listView1.Items.Add(new ListViewItem(row[0].ToString(), 0));
                }
            }
            catch (Exception)
            {
                this.cbbDBNames.Items.Remove(this.cbbDBNames.SelectedItem);
            }
        }

        // 把字符串数组转成int型数组
        private int[] ConvertToIntArray(string[] args)
        {
            int[] values = new int[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                if (!string.IsNullOrEmpty(args[i].Trim()))
                {
                    values[i] = Convert.ToInt32(args[i].Trim());
                }
            }
            return values;
        }

        // 获取当前表字段信息
        private List<FieldInfo> GetColumns(string tableName, ref string pk, ref int height)
        {
            List<FieldInfo> list = new List<FieldInfo>();

            StringBuilder sql = new StringBuilder();

            sql.Append("SELECT");
            sql.Append("      C.NAME AS FieldName");
            sql.Append("     ,T.NAME AS FieldType");
            sql.Append("     ,CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM SYSOBJECTS WHERE XTYPE='PK' AND PARENT_OBJ=C.ID AND NAME IN (SELECT NAME FROM SYSINDEXES WHERE INDID IN(SELECT INDID FROM SYSINDEXKEYS WHERE ID = C.ID AND COLID=C.COLID))) THEN 1 ELSE 0 END) AS IsPrimaryKey");
            sql.Append("     ,CONVERT(BIT,COLUMNPROPERTY(C.ID,C.NAME,'ISIDENTITY')) AS IsAutomatic");
            sql.Append("     ,COLUMNPROPERTY(C.ID,C.NAME,'PRECISION') AS FieldLength");
            sql.Append("     ,ISNULL(COLUMNPROPERTY(C.ID,C.NAME,'SCALE'),0) AS DecimalPlaces");
            sql.Append(" FROM SYSCOLUMNS C INNER JOIN SYSTYPES T ON C.XUSERTYPE = T.XUSERTYPE ");
            sql.AppendFormat("WHERE C.ID = OBJECT_ID('{0}')", tableName);

            SqlConnection conn = new SqlConnection(string.Format("Data Source={0};User ID={1};Password={2};database={3}", this.cbxDBServerList.SelectedItem, this.textBox1.Text, this.textBox2.Text, this.cbbDBNames.SelectedItem));

            SqlDataAdapter sda = new SqlDataAdapter(sql.ToString(), conn);

            DataSet ds = new DataSet("DBS");

            conn.Open();

            sda.Fill(ds);

            sda.Dispose();

            conn.Close();

            var rows = ds.Tables[0].Rows;

            for (int i = 0; i < rows.Count; i++)
            {
                DataRow row = rows[i];

                if (i == 0)
                {
                    pk = row[0].ToString();
                }
                if (Convert.ToBoolean(row[2]))
                {
                    pk = row[0].ToString();
                }
                list.Add(new FieldInfo()
                {
                    FieldName = row[0].ToString(),
                    FieldType = row[1].ToString(),
                    IsPrimaryKey = Convert.ToBoolean(row[2]),
                    IsAutomatic = Convert.ToBoolean(row[3]),
                    FieldLength = Convert.ToInt32(row[4]),
                    DecimalPlaces = Convert.ToInt32(row[5])
                });

                if (!Convert.ToBoolean(row[2]))
                {
                    switch (row[1].ToString())
                    {
                        case "text":
                        case "ntext":
                            height += 100;
                            break;
                        default:
                            height += 30;
                            break;
                    }
                }
            }

            if (height > 600)
            {
                height = 600;
            }

            return list;
        }

        // 转换成驼峰命名
        private string GetClassName(string tableName, string prefix)
        {
            tableName = tableName.ToLower().Remove(0, prefix.Length);

            char[] tableNameChars = tableName.ToArray();

            StringBuilder sb = new StringBuilder();

            bool startWord = true;

            for (int i = 0; i < tableNameChars.Length; i++)
            {
                if (this.textBox4.Text.ToUpper() != tableNameChars[i].ToString().ToUpper())
                {
                    if (i == 0 || startWord)
                    {
                        sb.Append(tableNameChars[i].ToString().ToUpper());
                        startWord = false;
                    }
                    else
                    {
                        sb.Append(tableNameChars[i].ToString().ToLower());
                    }
                }
                else
                {
                    startWord = true;
                }
            }

            return sb.ToString();
        }

        private Dictionary<string, int> GetPrefixNumbers()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            //System.Collections.Specialized.NameValueCollection nvc = ConfigurationManager.AppSettings;

            //foreach (string key in nvc.AllKeys)
            //{
            //    try
            //    {
            //        dict.Add(key, Convert.ToInt32(nvc[key]));
            //    }
            //    catch (Exception)
            //    {
            //        continue;
            //    }
            //}

            return dict;
        }

        private void LogConfig(string p)
        {
            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            if (!config.HasFile)
            {
                System.IO.FileStream fs = new FileStream(config.FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

                //书写器
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                sw.WriteLine("<configuration>");
                sw.WriteLine("  <appSettings>");
                sw.WriteLine("  </appSettings>");
                sw.WriteLine("</configuration>");

                sw.Close();
                fs.Close();
            }
            else
            {
                //清除记录
                config.AppSettings.Settings.Clear();
            }

            //路径记忆
            ModifyAppSettings("路径", p);
            //服务器
            ModifyAppSettings("服务器", this.cbxDBServerList.SelectedItem == null ? this.cbxDBServerList.Text : this.cbxDBServerList.SelectedItem.ToString());
            //用户名
            ModifyAppSettings("用户名", this.textBox1.Text);
            //密码
            ModifyAppSettings("密码", this.textBox2.Text);
            //数据库
            ModifyAppSettings("数据库", this.cbbDBNames.SelectedItem.ToString());
            //----------------------------------------------------------------------
            //跟命名空间
            ModifyAppSettings("跟命名空间", this.tbBaseNamespace.Text);
            //列表母版页
            ModifyAppSettings("列表母版页", this.textBox7.Text);
            //窗口母版页
            ModifyAppSettings("窗口母版页", this.textBox8.Text);
            //自动前缀
            ModifyAppSettings("自动前缀", this.checkBox1.Checked.ToString());
            //前缀
            ModifyAppSettings("tbPrefix", this.tbPrefix.Text);
            //表名前缀分隔符
            ModifyAppSettings("表名前缀分隔符", this.textBox4.Text);
            //编号开始
            ModifyAppSettings("编号开始", this.textBox3.Text);
            //编号结束
            ModifyAppSettings("编号结束", this.textBox5.Text);
            //排除编号
            ModifyAppSettings("排除编号", this.textBox6.Text);
            //----------------------------------------------------------------------
            //生成列表页面
            ModifyAppSettings("生成列表页面", this.checkBox5.Checked.ToString());
            //生成新增页面
            ModifyAppSettings("生成新增页面", this.checkBox2.Checked.ToString());
            //生成修改页面
            ModifyAppSettings("生成修改页面", this.checkBox3.Checked.ToString());
            //生成删除页面
            ModifyAppSettings("生成删除页面", this.checkBox4.Checked.ToString());
        }

        private void ModifyAppSettings(string key, string value)
        {
            config.AppSettings.Settings.Remove(key);

            config.AppSettings.Settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");

            XmlDocument xDoc = new XmlDocument();

            xDoc.Load(System.Windows.Forms.Application.ExecutablePath + ".config");

            XmlNode xNode;

            XmlElement xElem1;

            XmlElement xElem2;

            xNode = xDoc.SelectSingleNode("//appSettings");

            xElem1 = (XmlElement)xNode.SelectSingleNode(@"//add[@key="" + key + ""]");

            if (xElem1 != null)
            {
                xElem1.SetAttribute("value", value);
            }
            else
            {
                xElem2 = xDoc.CreateElement("add");

                xElem2.SetAttribute("key", key);

                xElem2.SetAttribute("value", value);

                xNode.AppendChild(xElem2);
            }

            xDoc.Save(System.Windows.Forms.Application.ExecutablePath + ".config");
        }

        //获取前缀
        private string GetPrefix(string tableName)
        {
            //前缀     tbPrefix
            string prefix = this.tbPrefix.Text.ToUpper();

            //表名分隔符
            char[] tbNameSplit = this.textBox4.Text.ToCharArray();

            if (this.checkBox1.Checked)
            {
                if (tbNameSplit.Length > 0) //如果有分隔符
                {
                    foreach (char chr in tbNameSplit)
                    {
                        prefix = tableName.Split(chr)[0];
                        prefix = prefix.Substring(0, 1).ToUpper() + prefix.Substring(1).ToLower();
                        break;
                    }
                }
                else
                {
                    char[] tableNameChars = tableName.ToArray();

                    StringBuilder sb = new StringBuilder();

                    bool startWord = true;

                    for (int i = 0; i < tableNameChars.Length; i++)
                    {
                        if (this.textBox4.Text != tableNameChars[i].ToString())
                        {
                            if (i == 0 || startWord)
                            {
                                sb.Append(tableNameChars[i].ToString().ToUpper());
                                startWord = false;
                            }
                            else
                            {
                                sb.Append(tableNameChars[i].ToString().ToLower());
                            }
                        }
                        else
                        {
                            startWord = true;
                        }
                    }

                    prefix = sb.ToString();

                }
            }
            if (prefix.Length > 4)
            {
                prefix = prefix.Substring(0, 4);
            }

            return prefix;
        }

        //获取文件编号
        private int GetFileNumber(string prefix)
        {
            //当前编号
            int currentNum = 1;
            //编号开始 textBox3
            int startNum = Convert.ToInt32(this.textBox3.Text);
            //编号结束 textBox5
            int endNum = Convert.ToInt32(this.textBox5.Text);
            //排除编号 textBox6
            int[] excludeNum = ConvertToIntArray(this.textBox6.Text.Split(','));

            //前缀和文件编号
            Dictionary<string, int> PrefixNumbers = GetPrefixNumbers();

            if (PrefixNumbers.ContainsKey(prefix))
            {
                currentNum = (int)PrefixNumbers[prefix];
            }
            else
            {
                PrefixNumbers[prefix] = currentNum;
            }

            bool exist = false;

            foreach (int exclude in excludeNum)
            {
                if (currentNum + 1 == exclude)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist) //如果不存在
            {
                PrefixNumbers[prefix] = ++currentNum; //记录当前号
            }
            return currentNum;
        }

        //初始化控件的值
        private void InitConfig(string key, TextBox ctrl)
        {
            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            if (config.AppSettings.Settings.AllKeys.Contains(key))
            {
                ctrl.Text = config.AppSettings.Settings[key].Value;
            }
        }

        //初始化控件的值
        private void InitConfig(string key, ComboBox ctrl)
        {
            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            if (config.AppSettings.Settings.AllKeys.Contains(key))
            {
                var value = config.AppSettings.Settings[key].Value;

                ctrl.Text = value;

                ctrl.SelectedItem = value;

                ctrl.SelectedIndex = 0;
            }
        }

        //初始化控件的值
        private void InitConfig(string key, CheckBox ctrl)
        {
            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
            if (config.AppSettings.Settings.AllKeys.Contains(key))
            {
                ctrl.Checked = Convert.ToBoolean(config.AppSettings.Settings[key]);
            }
        }

        //设置隐藏
        private void SetEnabled(bool enabled)
        {
            this.btnStart.Enabled = enabled;
            this.cbxDBServerList.Enabled = enabled;
            this.textBox1.Enabled = enabled;
            this.textBox2.Enabled = enabled;
            this.cbbDBNames.Enabled = enabled;
            this.tbBaseNamespace.Enabled = enabled;
            this.textBox7.Enabled = enabled;
            this.textBox8.Enabled = enabled;
            this.checkBox1.Enabled = enabled;
            this.tbPrefix.Enabled = enabled;
            this.textBox4.Enabled = enabled;
            this.textBox3.Enabled = enabled;
            this.textBox5.Enabled = enabled;
            this.textBox6.Enabled = enabled;
            this.checkBox5.Enabled = enabled;
            this.checkBox2.Enabled = enabled;
            this.checkBox3.Enabled = enabled;
            this.checkBox4.Enabled = enabled;
            this.listView1.Enabled = enabled;
        }

        #endregion

        #region 生成页面代码

        //页面后台
        private void GenerateListPageCS(string baseNameSpace, string prefix, string className, string folder)
        {
            //列表文件路径
            string filePath = Path.Combine(folder, className + "PageList.aspx.cs");

            System.IO.FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine("using LT.Component.Utility;");
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine("using System.Linq;");
            sw.WriteLine("using System.Web;");
            sw.WriteLine("using System.Web.UI;");
            sw.WriteLine("using System.Web.UI.WebControls;");
            sw.WriteLine("");
            sw.WriteLine("namespace " + baseNameSpace + ".Web." + prefix + "");
            sw.WriteLine("{");
            sw.WriteLine("    public partial class " + className + " : " + baseNameSpace + ".Web.BaseListPage");
            sw.WriteLine("    {");
            sw.WriteLine("        protected void Page_Load(object sender, EventArgs e)");
            sw.WriteLine("        {");
            sw.WriteLine("            if (\"GetData\" == base.action) GetData();");
            sw.WriteLine("        }");
            sw.WriteLine("");
            sw.WriteLine("        private void GetData()");
            sw.WriteLine("        {");
            sw.WriteLine("            var list = Provider." + prefix + "." + className + ".Instance.GetPagerList(query);");
            sw.WriteLine("            var griddata = new { Rows = list, Total = query.Record };");
            sw.WriteLine("            Response.Write(Strings.SerializeToJSON(griddata));");
            sw.WriteLine("            Response.End();");
            sw.WriteLine("        }");
            sw.WriteLine("    }");
            sw.WriteLine("}");

            sw.Close();
            fs.Close();
        }

        //生成页面设计文件
        private void GenerateListPageDesigner(string baseNameSpace, string prefix, string className, string folder)
        {
            //列表文件路径
            string filePath = Path.Combine(folder, className + "PageList.aspx.designer.cs");

            System.IO.FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine("//------------------------------------------------------------------------------");
            sw.WriteLine("// <auto-generated>");
            sw.WriteLine("//     This code was generated by a tool.");
            sw.WriteLine("//");
            sw.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
            sw.WriteLine("//     the code is regenerated. ");
            sw.WriteLine("// </auto-generated>");
            sw.WriteLine("//------------------------------------------------------------------------------");
            sw.WriteLine("");
            sw.WriteLine("namespace " + baseNameSpace + ".Web." + prefix + " {");
            sw.WriteLine("    ");
            sw.WriteLine("    public partial class " + className + " {}");
            sw.WriteLine("}");

            sw.Close();
            fs.Close();
        }

        //生成列表页面
        private void GenerateListPage(string baseNameSpace, string prefix, string tableName, string className, string folder, string pk, int height, List<FieldInfo> columns)
        {
            //列表文件路径
            string listPagePath = Path.Combine(folder, className + "PageList.aspx");

            //编辑/新增页面路径
            string actPagePath = Path.Combine(folder, className + "Act.aspx");

            System.IO.FileStream fs = new FileStream(listPagePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            bool showToolBar = (this.checkBox2.Checked || this.checkBox3.Checked || this.checkBox4.Checked);

            sw.WriteLine("<%@ Page Title=\"\" Language=\"C#\" MasterPageFile=\"" + this.textBox8.Text + "\" AutoEventWireup=\"true\" CodeBehind=\"" + className + ".aspx.cs\" Inherits=\"" + baseNameSpace + ".Web." + prefix + "." + className + "\" %>");
            sw.WriteLine("");
            sw.WriteLine("<asp:Content ID=\"Content1\" ContentPlaceHolderID=\"ContentPlaceHolder\" runat=\"server\">");
            sw.WriteLine("    <script type=\"text/javascript\">                                              ");
            sw.WriteLine("        var g;                                                                     ");
            if (showToolBar)
            {
                sw.WriteLine("        var toolbar = [                                                            ");
                if (this.checkBox2.Checked) //新增
                {
                    sw.WriteLine("                { text: '增加', click: add, icon: 'add' }, { line: true },     ");
                }
                if (this.checkBox3.Checked) //修改
                {
                    sw.WriteLine("                { text: '修改', click: edit, icon: 'modify' }, { line: true },  ");
                }
                if (this.checkBox4.Checked) //删除
                {
                    sw.WriteLine("                { text: '删除', click: del, icon: 'delete'},                  ");
                }
                sw.WriteLine("                { line: true }");
                sw.WriteLine("        ];                                                                         ");
            }

            sw.WriteLine("        $(document).ready(function () {                                            ");
            sw.WriteLine("            g = $(\"#maingrid\").ligerGrid({                                       ");
            sw.WriteLine("                columns: [                                                         ");
            //----------------------------
            int kkk = columns.Count;
            foreach (var column in columns)
            {
                if (!column.IsPrimaryKey)
                {
                    if (kkk > 1)
                    {
                        sw.WriteLine("                    { minWidth :100, display: '" + column.FieldName + "', name: '" + column.FieldName + "' }, ");
                    }
                    else
                    {
                        sw.WriteLine("                    { minWidth :100,display: '" + column.FieldName + "', name: '" + column.FieldName + "' }  ");
                    }
                }
                kkk--;
            }
            //----------------------------
            sw.WriteLine("                ],                                                                 ");
            if (showToolBar)
            {
                sw.WriteLine("                toolbar: { items: toolbar },                                       ");
            }
            sw.WriteLine("                url: '" + className + "PageList.aspx?action=GetData',                      ");
            sw.WriteLine("                identity: '" + pk + "'                                             ");
            sw.WriteLine("            });                                                                    ");
            sw.WriteLine("            $(\"#maingrid\").focus();                                              ");
            sw.WriteLine("        });                                                                        ");
            sw.WriteLine("");
            if (this.checkBox2.Checked) //新增
            {
                sw.WriteLine("        function add(o) {                                                          ");
                sw.WriteLine("            BtnLinkClick(g, \"新增\", '/" + prefix + "/" + className + "Act.aspx', 500, " + height + ", null);      ");
                sw.WriteLine("        }                                                                          ");
            }
            sw.WriteLine("");
            if (this.checkBox3.Checked) //修改
            {
                sw.WriteLine("        function edit(o) {                                                         ");
                sw.WriteLine("            BtnActClick(g, '修改', '/" + prefix + "/" + className + "Act.aspx', 500, " + height + ", 's', null);    ");
                sw.WriteLine("        }                                                                          ");
            }
            sw.WriteLine("");
            if (this.checkBox4.Checked) //删除
            {
                sw.WriteLine("        function del() {                                                           ");
                sw.WriteLine("            BtnDelClick(g, '" + prefix + "." + className + "Model'); ");
                sw.WriteLine("        }                                                                          ");
            }
            sw.WriteLine("    </script>                                                                      ");
            sw.WriteLine("    <div id=\"maingrid\" style=\"margin-top: 0px\"></div>                          ");
            sw.WriteLine("    <div style=\"display: none;\"></div>                                           ");
            sw.WriteLine("</asp:Content>                                                                     ");

            sw.Close();
            fs.Close();
        }

        //新增页面后台页面
        private void GenerateAddPageCS(string baseNameSpace, string prefix, string className, string folder, string pk, List<FieldInfo> columns)
        {
            //编辑/新增页面路径
            string actPagePath = Path.Combine(folder, className + "Act.aspx.cs");

            System.IO.FileStream fs = new FileStream(actPagePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine("using " + baseNameSpace + ".Model." + prefix + ";");
            sw.WriteLine("using LT.Component.Utility;");
            sw.WriteLine("using LT.Component.Web;");
            sw.WriteLine("using System;");
            sw.WriteLine("");
            sw.WriteLine("namespace " + baseNameSpace + ".Web." + prefix + "");
            sw.WriteLine("{");
            sw.WriteLine("    public partial class " + className + "Act : " + baseNameSpace + ".Web.BasePage");
            sw.WriteLine("    {");
            sw.WriteLine("        protected void Page_Load(object sender, EventArgs e)");
            sw.WriteLine("        {");
            sw.WriteLine("            if (IsPostBack)");
            sw.WriteLine("            {");
            sw.WriteLine("                HttpPagePostback();");
            sw.WriteLine("            }");
            sw.WriteLine("            else");
            sw.WriteLine("            {");
            sw.WriteLine("                LoadCurrentPage();");
            sw.WriteLine("            }");
            sw.WriteLine("        }");
            sw.WriteLine("");
            sw.WriteLine("        private void LoadCurrentPage()");
            sw.WriteLine("        {");
            sw.WriteLine("            int id = PageHelper.GetIdFromUrl(\"id\");");
            sw.WriteLine("");
            sw.WriteLine("            if (id > 0)");
            sw.WriteLine("            {");
            sw.WriteLine("                var model = " + baseNameSpace + ".Provider." + prefix + "." + className + ".Instance.GetSingle(id, null, null);");
            sw.WriteLine("");
            sw.WriteLine("                if (model != null)");
            sw.WriteLine("                {");
            foreach (var item in columns)
            {
                if (!item.IsPrimaryKey)
                {
                    switch (item.FieldType)
                    {
                        case "date":
                        case "time":
                        case "datetime":
                        case "datetime2":
                        case "smalldatetime":
                        case "datetimeoffset":
                            sw.WriteLine("                    this.tb" + item.FieldName + ".Text = Strings.ConvertToString(model." + item.FieldName + ");");
                            break;
                        case "bit":
                            sw.WriteLine("                    this.ckb" + item.FieldName + ".Checked = model." + item.FieldName + ";");
                            break;
                        default:
                            sw.WriteLine("                    this.tb" + item.FieldName + ".Text = model." + item.FieldName + ".ToString();");
                            break;
                    }
                }
            }
            sw.WriteLine("                }");
            sw.WriteLine("            }");
            sw.WriteLine("        }");
            sw.WriteLine("");
            sw.WriteLine("        private void HttpPagePostback()");
            sw.WriteLine("        {");
            sw.WriteLine("            int id = PageHelper.GetIdFromUrl(\"id\");");
            sw.WriteLine("");
            sw.WriteLine("            if (id > 0)");
            sw.WriteLine("            {");
            sw.WriteLine("                var model = " + baseNameSpace + ".Provider." + prefix + "." + className + ".Instance.GetSingle(id, null, null);");
            sw.WriteLine("");
            sw.WriteLine("                if (model != null)");
            sw.WriteLine("                {");
            foreach (var item in columns)
            {
                if (!item.IsPrimaryKey)
                {
                    switch (item.FieldType)
                    {
                        case "date":
                        case "time":
                        case "datetime":
                        case "datetime2":
                        case "smalldatetime":
                        case "datetimeoffset":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToDateTime(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "tinyint":
                        case "smallint":
                        case "int":
                        case "bigint":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToInt(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "decimal":
                        case "money":
                        case "float":
                        case "numeric":
                        case "smallmoney":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToDecimal(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "bit":
                            sw.WriteLine("                    model." + item.FieldName + " = this.ckb" + item.FieldName + ".Checked;");
                            break;
                        case "varchar":
                        case "char":
                        case "nvarchar":
                        case "nchar":
                        case "text":
                        case "ntext":
                        default:
                            sw.WriteLine("                    model." + item.FieldName + " = this.tb" + item.FieldName + ".Text;");
                            break;
                    }
                }
            }
            sw.WriteLine("");
            sw.WriteLine("                    " + baseNameSpace + ".Provider." + prefix + "." + className + ".Instance.Update(model);");
            sw.WriteLine("");
            sw.WriteLine("                    this.SubmitOK();");
            sw.WriteLine("                }");
            sw.WriteLine("            }");
            sw.WriteLine("            else");
            sw.WriteLine("            {");
            sw.WriteLine("                var model = new " + className + "Model();");
            sw.WriteLine("");
            sw.WriteLine("                if (model != null)");
            sw.WriteLine("                {");
            foreach (var item in columns)
            {
                if (!item.IsPrimaryKey)
                {
                    switch (item.FieldType)
                    {
                        case "date":
                        case "time":
                        case "datetime":
                        case "datetime2":
                        case "smalldatetime":
                        case "datetimeoffset":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToDateTime(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "tinyint":
                        case "smallint":
                        case "int":
                        case "bigint":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToInt(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "decimal":
                        case "money":
                        case "float":
                        case "numeric":
                        case "smallmoney":
                            sw.WriteLine("                    model." + item.FieldName + " = Strings.ConvertToDecimal(this.tb" + item.FieldName + ".Text);");
                            break;
                        case "bit":
                            sw.WriteLine("                    model." + item.FieldName + " = this.ckb" + item.FieldName + ".Checked;");
                            break;
                        case "varchar":
                        case "char":
                        case "nvarchar":
                        case "nchar":
                        case "text":
                        case "ntext":
                        default:
                            sw.WriteLine("                    model." + item.FieldName + " = this.tb" + item.FieldName + ".Text;");
                            break;
                    }
                }
            }
            sw.WriteLine("");
            sw.WriteLine("                    " + baseNameSpace + ".Provider." + prefix + "." + className + ".Instance.Insert(model);");
            sw.WriteLine("");
            sw.WriteLine("                    this.SubmitOK();");
            sw.WriteLine("                }");
            sw.WriteLine("            }");
            sw.WriteLine("        }");
            sw.WriteLine("    }");
            sw.WriteLine("}");

            sw.Close();
            fs.Close();
        }

        //新增页面设计页面
        private void GenerateAddPageDesigner(string baseNameSpace, string prefix, string className, string folder, List<FieldInfo> columns)
        {
            //编辑/新增页面路径
            string actPagePath = Path.Combine(folder, className + "Act.aspx.designer.cs");

            System.IO.FileStream fs = new FileStream(actPagePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine("namespace " + baseNameSpace + ".Web." + prefix + " {");
            sw.WriteLine("    public partial class " + className + "Act {");
            foreach (var item in columns)
            {
                if (!item.IsPrimaryKey)
                {
                    switch (item.FieldType)
                    {
                        case "bit":
                            sw.WriteLine("        protected global::System.Web.UI.WebControls.CheckBox ckb" + item.FieldName + ";");
                            break;
                        default:
                            sw.WriteLine("        protected global::System.Web.UI.WebControls.TextBox tb" + item.FieldName + ";");
                            break;
                    }
                }
            }
            sw.WriteLine("        protected global::" + baseNameSpace + ".WebControls.ActButtons ActButtons1;");
            sw.WriteLine("    }");
            sw.WriteLine("}");

            sw.Close();
            fs.Close();
        }

        //新增页面
        private void GenerateAddPage(string baseNameSpace, string prefix, string className, string folder, string pk, List<FieldInfo> columns, int height)
        {
            //编辑/新增页面路径
            string actPagePath = Path.Combine(folder, className + "Act.aspx");

            System.IO.FileStream fs = new FileStream(actPagePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            //书写器
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

            sw.WriteLine("<%@ Page Language=\"C#\" AutoEventWireup=\"true\" MasterPageFile=\"" + this.textBox8.Text + "\" CodeBehind=\"" + className + "Act.aspx.cs\" Inherits=\"" + baseNameSpace + ".Web." + prefix + "." + className + "Act\" %>");
            sw.WriteLine("");
            sw.WriteLine("<asp:Content ID=\"Content1\" ContentPlaceHolderID=\"ContentPlaceHolder\" runat=\"server\">");
            sw.WriteLine("<div style=\"overflow: auto; height: " + (height - 50) + "px;\">");
            sw.WriteLine("    <table cellpadding=\"0\" cellspacing=\"0\" class=\"wintable\">");

            //TODO:字段太多咋办
            foreach (var item in columns)
            {
                if (!item.IsPrimaryKey)
                {
                    sw.WriteLine("        <tr>");
                    sw.WriteLine("            <th>" + item.FieldName + "：</th>");
                    sw.WriteLine("            <td>");

                    switch (item.FieldType)
                    {
                        case "date":
                        case "time":
                        case "datetime":
                        case "datetime2":
                        case "smalldatetime":
                        case "datetimeoffset":
                            sw.WriteLine("                <asp:TextBox ID=\"tb" + item.FieldName + "\" runat=\"server\"  CssClass=\"Wdate\" onfocus=\"WdatePicker()\" />");
                            break;
                        case "tinyint":
                        case "smallint":
                        case "int":
                        case "bigint":
                            sw.WriteLine("                <asp:TextBox ID=\"tb" + item.FieldName + "\" runat=\"server\"  vformat=\"decimal\" MaxLength=\"" + item.FieldLength + "\" msg=\"只能输入数字，最多" + item.FieldLength + "位整数\" />");
                            break;
                        case "decimal":
                        case "money":
                        case "float":
                        case "numeric":
                        case "smallmoney":
                            sw.WriteLine("                <asp:TextBox ID=\"tb" + item.FieldName + "\" runat=\"server\" CssClass=\"input\" vformat=\"decimal\" MaxLength=\"" + item.FieldLength + "\" vlen=\"0," + item.FieldLength + "\" vmsg=\"只能输入数字，最多8位整数，2位小数\" />");
                            break;
                        case "bit":
                            sw.WriteLine("                <asp:CheckBox ID=\"ckb" + item.FieldName + "\" runat=\"server\" Text=\"" + item.FieldName + "\" Checked=\"true\" />");
                            break;
                        default:
                        case "varchar":
                        case "char":
                        case "nvarchar":
                        case "nchar":
                            sw.WriteLine("                <asp:TextBox ID=\"tb" + item.FieldName + "\" runat=\"server\" CssClass=\"input\" MaxLength=\"" + item.FieldLength + "\" vlen=\"0," + item.FieldLength + "\" />");
                            break;
                        case "text":
                        case "ntext":
                            sw.WriteLine("                 <asp:TextBox ID=\"tb" + item.FieldName + "\" runat=\"server\" Width=\"95%\" Height=\"100px\" TextMode=\"MultiLine\" maxlen=\"" + item.FieldLength + "\" vlen=\"1," + item.FieldLength + "\" />");
                            break;
                    }
                    sw.WriteLine("            </td>");
                    sw.WriteLine("        </tr>");
                }
            }
            sw.WriteLine("      </table>");
            sw.WriteLine("    </div>");
            sw.WriteLine("    <div class=\"actButtons\">");
            sw.WriteLine("        <xc:ActButtons ID=\"ActButtons1\" runat=\"server\" />");
            sw.WriteLine("    </div>");
            sw.WriteLine("    <script type=\"text/javascript\">");
            sw.WriteLine("        var validator = $('form').FormValidtor();");
            sw.WriteLine("        function SubmitFormCtrl(f) {");
            sw.WriteLine("            if (validator.Check()) {");
            sw.WriteLine("                $(f).ajaxSubmit({ beforeSubmit: showRequest, success: showResponse });");
            sw.WriteLine("            }");
            sw.WriteLine("            return false;");
            sw.WriteLine("        }");
            sw.WriteLine("    </script>");
            sw.WriteLine("</asp:Content>");

            sw.Close();
            fs.Close();
        }

        #endregion

    }
}
