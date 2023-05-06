using Npgsql;
using Newtonsoft.Json;
using System.Reflection;


//定义数据库连接相关参数


namespace restaurantSystemServer
{

    #region restaurant数据库中各表对应的obj
    [Serializable]public class Tadmin    //admin
    {
        public int id;
        public string name;
        public string phone;

        public Tadmin() { }
        public Tadmin(int _id, string _name, string _phone) { id = _id; name = _name; phone = _phone; }
    }
    [Serializable]public class Tcustomer  //customer
    {
        public int id;
        public string name;
        public string phone;
        public Tcustomer() { }
        public Tcustomer(int _id, string _name, string _phone)
        { id = _id; name = _name; phone = _phone; }
    }
    [Serializable]public class Tinventory  //inventory
    {
        public int id;
        public int remain;
        public Tinventory() { }
        public Tinventory(int _id, int _remain)
        { id = _id; remain = _remain; }
    }
    [Serializable]
    public class Tmenu  //menu
    {
        public int id;
        public string food;
        public string description;
        public Tmenu() { }
        public Tmenu(int _id, string _food, string _description) { id = _id; food = _food; description = _description; }

    }
    [Serializable]
    public class Tsize  //size
    {
        public int id;
        public int mu_id;
        public float price;
        public string size;


        public Tsize() { }
        public Tsize(int _id, int _mu_id, string _size, float _price)
        { id = _id; mu_id = _mu_id; size = _size; price = _price; }
    }

    //用于顾客菜单视图的数据结构
    [Serializable]public class VcusMenu
    {
        public int s_id;
        public string mu_food;
        public string s_size;
        public float s_price;
        public string mu_desc;

        public VcusMenu() { }
        public VcusMenu(int _id, string _food, string _size, float _price, string _desc)
        { s_id = _id; mu_food = _food; s_size = _size; s_price = _price; mu_desc = _desc; }
    }


    [Serializable]
    public class Torder  //order
    {
        public int id;
        public int cus_id;
        public float price;
        public string address;
        public Torder() { }
        public Torder(int _id, int _cus_id, float _price, string _address)
        { id = _id; cus_id = _cus_id; price = _price; address = _address; }
    }

    [Serializable]
    public class Tlist
    {
        public int o_id;
        public int s_id;
        public Tlist() { }
        public Tlist(int _o_id, int _s_id)
        { o_id = _o_id; s_id = _s_id; }
    }

    #endregion

    class DataBase
    {
        public enum SQLType
        {
            INSERT, //增
            DELETE, //删
            UPDATE, //改
            SELECT  //查
        }

        //public static MethodInfo mt;
        /// <summary>
        /// 根据Ins类型确定对应的执行SQL语句的方法
        /// </summary>
        private static Dictionary<JsonDisposer.Ins, string> FuncDic;
        /// <summary>
        /// 根据Ins确定对应SQL语句的类型
        /// </summary>
        private static Dictionary<JsonDisposer.Ins, SQLType> TypeDic;

        public static Type t = typeof(DataBase);

        public static void InitDic()
        {
            //初始化方法字典
            FuncDic = new Dictionary<JsonDisposer.Ins, string>
            {
                { JsonDisposer.Ins.select_Login_Admin, "Select_admin"},
                { JsonDisposer.Ins.select_Login_Cus, "Select_customer"},
                { JsonDisposer.Ins.insert_Sign_Cus,"AlterTable"},

                { JsonDisposer.Ins.select_All_Inventory,"Select_inventory"},
                { JsonDisposer.Ins.update_Inventory,"AlterTable"},

                { JsonDisposer.Ins.select_All_Menu, "Select_menu"},
                { JsonDisposer.Ins.insert_Menu,"AlterTable"},
                { JsonDisposer.Ins.delete_Menu,"AlterTable"},
                { JsonDisposer.Ins.update_Menu,"AlterTable"},

                { JsonDisposer.Ins.select_All_size, "Select_size"},
                { JsonDisposer.Ins.insert_Size,"AlterTable"},
                { JsonDisposer.Ins.update_Size,"AlterTable"},

                { JsonDisposer.Ins.select_cusMenuView,"Select_cusMenuView"},
                { JsonDisposer.Ins.insert_Order,"AlterTable"},
                { JsonDisposer.Ins.insert_List,"AlterTable"},

            };

            //初始化类型字典
            TypeDic = new Dictionary<JsonDisposer.Ins, SQLType>
            {
                { JsonDisposer.Ins.select_Login_Admin,SQLType.SELECT},
                { JsonDisposer.Ins.select_Login_Cus,SQLType.SELECT},

                { JsonDisposer.Ins.insert_Sign_Cus,SQLType.INSERT},
                { JsonDisposer.Ins.select_All_Inventory,SQLType.SELECT},
                { JsonDisposer.Ins.update_Inventory,SQLType.UPDATE},

                { JsonDisposer.Ins.select_All_Menu, SQLType.SELECT},
                { JsonDisposer.Ins.insert_Menu,SQLType.INSERT},
                { JsonDisposer.Ins.delete_Menu,SQLType.DELETE},
                { JsonDisposer.Ins.update_Menu,SQLType.UPDATE},

                { JsonDisposer.Ins.select_All_size,SQLType.SELECT},
                { JsonDisposer.Ins.insert_Size,SQLType.INSERT},
                { JsonDisposer.Ins.update_Size,SQLType.UPDATE},

                { JsonDisposer.Ins.select_cusMenuView,SQLType.SELECT},
                { JsonDisposer.Ins.insert_Order,SQLType.INSERT},
                { JsonDisposer.Ins.insert_List,SQLType.INSERT}
            };
        }

        /// <summary>
        /// 获取待使用的SQL指令类型
        /// </summary>
        /// <param name="_ins"></param>
        /// <returns></returns>
        public static SQLType GetSQLType(JsonDisposer.Ins _ins)
        {
            return TypeDic[_ins];
        }

        //配置数据库连接，返回数据库连接
        public static NpgsqlConnection GetConnection()
        {
            string port = "26000";
            string host = "192.168.56.101";
            string username = "jiang";
            string password = "jiang@123";
            string dbname = "restaurant";
            string conStr = string.Format("PORT={0};DATABASE={1};HOST={2};PASSWORD={3};USERID={4};Pooling=false", port, dbname, host, password, username);

            //Console.WriteLine(conStr);
            //配置数据库连接字符串
            NpgsqlConnection npgsqlCon = new NpgsqlConnection(conStr);  //生成数据库连接
            return npgsqlCon;
        }

        //打印版本信息
        public static void printVersion()
        {
            using var con = GetConnection();
            con.Open();
            string sqlStr = "SELECT version()";
            using var cmd = new NpgsqlCommand(sqlStr, con);
            Console.WriteLine($"数据库版本信息为{cmd.ExecuteScalar()?.ToString()}\n");
            con.Close();
        }


        /// <summary>
        /// 执行Select查找类sql,根据ins确定数据库会返回的值，并调用对应的接收函数
        /// </summary>
        /// <param name="_sql"></param>
        /// <param name="_ins"></param>
        /// <returns></returns>
        public static void ExecuteSelectSQL(string _sql, JsonDisposer.Ins _ins)
        {
            try
            {
                using var con = GetConnection();
                con.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = con;
                cmd.CommandText = _sql;

                //根据ins判断要执行的方法
                MethodInfo mt = t.GetMethod(FuncDic[_ins]);
                mt.Invoke(null, new object[] { cmd.ExecuteReader() });
                con.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }



        /// <summary>
        /// 执行insert/update/delete类sql
        /// </summary>
        /// <param name="_sql"></param>
        /// <param name="_ins"></param>
        public static void ExecuteAlterSQL(string _sql, JsonDisposer.Ins _ins)
        {
            try
            {
                using var con = GetConnection();
                con.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = con;
                cmd.CommandText = _sql;
                AlterTable(cmd.ExecuteNonQuery());
                con.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 发送修改表格后的结果
        /// </summary>
        /// <param name="_count">（>=）1为成功，0为失败</param>
        public static void AlterTable(int _count)
        {
            Console.WriteLine($"受影响的行数{_count}");
            //count = 1表示插入成功 = 0表示插入失败
            Server.Instance.SendMsg(Server.Instance.client, _count.ToString());
        }

        /// <summary>
        /// 读取数据库中返回的Admin全属性结果，生成obj并转为Json
        /// </summary>
        /// <param name="_rdr"></param>
        /// <returns></returns>
        public static void Select_admin(NpgsqlDataReader _rdr)
        {
            //a_id  a_name  a_phone
            Tadmin admin = new();
            while (_rdr.Read())
            {
                admin.id = _rdr.GetInt16(0);
                admin.name = _rdr.GetString(1);
                admin.phone = _rdr.GetString(2);
            }
            string msg = JsonConvert.SerializeObject(admin);
            Console.WriteLine($"Tadmin转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }

        /// <summary>
        /// 读取数据库中返回的Tcustomer全属性结果，生成obj并转为Json
        /// </summary>
        /// <param name="_rdr"></param>
        /// <returns></returns>
        public static void Select_customer(NpgsqlDataReader _rdr)
        {
            //cus_id    cus_name    cus_phone
            Tcustomer cus = new();
            while (_rdr.Read())
            {
                cus.id = _rdr.GetInt16(0);
                cus.name = _rdr.GetString(1);
                cus.phone = _rdr.GetString(2);
            }

            string msg = JsonConvert.SerializeObject(cus);
            Console.WriteLine($"Tcustomer转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }

        /// <summary>
        /// 读取数据库中返回的Tinventory全属性结果，生成obj并转为Json
        /// </summary>
        /// <param name="_rdr"></param>
        /// <returns></returns>
        public static void Select_inventory(NpgsqlDataReader _rdr)
        {
            //i_id  i_remain
            List<Tinventory> inventoryList = new();
            while (_rdr.Read())
            {
                Tinventory tempInv = new();
                tempInv.id = _rdr.GetInt16(0);
                tempInv.remain = _rdr.GetInt16(1);
                inventoryList.Add(tempInv);
            }
            string msg = JsonConvert.SerializeObject(inventoryList);
            Console.WriteLine($"inventory转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }

        public static void Select_menu(NpgsqlDataReader _rdr)
        {
            //i_id  i_remain
            List<Tmenu> List = new();
            while (_rdr.Read())
            {
                Tmenu temp = new();
                temp.id = _rdr.GetInt16(0);
                temp.food = _rdr.GetString(1);
                temp.description = _rdr.GetString(2);
                List.Add(temp);
            }
            string msg = JsonConvert.SerializeObject(List);
            Console.WriteLine($"Menu转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }


        public static void Select_size(NpgsqlDataReader _rdr)
        {
            //s_id  s_mu_id s_size  s_price  i_remain
            List<Tsize> List = new();
            while (_rdr.Read())
            {
                Tsize temp = new();
                temp.id = _rdr.GetInt32(0);
                temp.mu_id = _rdr.GetInt16(1);
                temp.price = _rdr.GetFloat(2);
                temp.size = _rdr.GetString(3);

                List.Add(temp);
            }
            string msg = JsonConvert.SerializeObject(List);
            Console.WriteLine($"Size转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }

        public static void Select_cusMenuView(NpgsqlDataReader _rdr)
        {
            //mu_id  mu_food    s_size  s_price mu_desc
            List<VcusMenu> List = new();
            while (_rdr.Read())
            {
                VcusMenu temp = new();
                temp.s_id = _rdr.GetInt32(0);
                temp.mu_food = _rdr.GetString(1);
                temp.s_size = _rdr.GetString(2);
                temp.s_price = _rdr.GetFloat(3);
                temp.mu_desc = _rdr.GetString(4);

                List.Add(temp);
            }
            string msg = JsonConvert.SerializeObject(List);
            Console.WriteLine($"VcusMenu转为Json格式的返回结果为：{msg}");
            //发送给客户端
            Server.Instance.SendMsg(Server.Instance.client, msg);
        }
    }
}
