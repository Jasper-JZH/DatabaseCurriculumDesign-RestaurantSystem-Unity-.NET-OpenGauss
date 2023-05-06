using Newtonsoft.Json.Linq;

namespace restaurantSystemServer
{
    public class JsonDisposer
    {
        //和Client中的Ins相对应，用于标识指令
        public enum Ins
        {
            select_Login_Admin,
            select_Login_Cus,
            insert_Sign_Cus,
            select_All_Inventory,
            update_Inventory,
            select_All_Menu,
            insert_Menu,
            delete_Menu,
            update_Menu,
            update_Size,
            select_All_size,
            insert_Size,
            select_cusMenuView,
            insert_Order,
            insert_List

        }
        /// <summary>
        /// 存储用于生成SQL指令的数据
        /// </summary>
        public class InsData
        {
            //属性字典
            public Dictionary<string, string> PDic;

            public InsData()
            { PDic = new Dictionary<string, string>(); }
        }

        /// <summary>
        /// 存储指令的编号和对应的函数引用
        /// </summary>
        public static Dictionary<Ins, Func<InsData, string>> iDir;

        /// <summary>
        /// 初始化成员字典
        /// </summary>
        public static void InitInstructionDic()
        {
            iDir = new Dictionary<Ins, Func<InsData, string>>
            {
                { Ins.select_Login_Admin,GetSQL_select_Login_Admin},
                { Ins.select_Login_Cus,GetSQL_select_Login_Cus},
                { Ins.insert_Sign_Cus,GetSQL_insert_Sign_Cus },

                { Ins.select_All_Inventory,GetSQL_select_All_Inventory},
                { Ins.update_Inventory,GetSQL_update_Inventory},

                { Ins.select_All_Menu,GetSQL_select_All_Menu},
                { Ins.insert_Menu,GetSQL_insert_Menu },
                { Ins.delete_Menu,GetSQL_delete_Menu},
                { Ins.update_Menu,GetSQL_update_Menu},

                { Ins.select_All_size,GetSQL_select_All_size},
                { Ins.insert_Size,GetSQL_insert_size},
                { Ins.update_Size,GetSQL_update_Size},

                { Ins.select_cusMenuView,GetSQL_select_cusMenuView},
                { Ins.insert_Order,GetSQL_insert_Order},
                { Ins.insert_List,GetSQL_insert_List},

            };
        }


        public static void DisposeInstructionFromJson2Objs(string _jsonStr, out Ins ins, out InsData insData)
        {
            Console.WriteLine("---------------开始解析---------------");

            insData = new();
            JObject jobj = JObject.Parse(_jsonStr);

            ins = (Ins)Convert.ToInt16(jobj["I"]);
            Console.WriteLine($"解析出【I】为{ins}");

            //解析属性P
            JArray jPAry = (JArray)jobj["P"];
            for (int i = 0; i < jPAry.Count; i++)
            {
                JToken jtk = (JToken)jPAry[i].First;    //因为设计的json格式中每个JObject中
                                                        //都只有一个key/value，所以可以直接用.First曲线救国
                JProperty jpt = (JProperty)jtk;
                string pNameStr = jpt.Name.ToString();
                string pValueStr = jpt.Value.ToString();
                Console.WriteLine($"解析出第{i}个属性{pNameStr}:{pValueStr}");

                insData.PDic.Add(pNameStr, pValueStr);
            }

            Console.WriteLine("------------------解析结束------------------");
        }

        /// <summary>
        /// 对外接口，调用方法解析收到的数据，并生成SQL
        /// </summary>
        /// <param name="_recMsg"></param>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public static Ins DisposeReceive(string _recMsg, out string _sql)
        {
            //接收并处理数据
            InsData insData;
            Ins ins;
            DisposeInstructionFromJson2Objs(_recMsg, out ins, out insData);
            //查找Ins字典索引对应的生成SQL的方法
            _sql = iDir[ins]?.Invoke(insData);
            return ins;
        }

        private static string GetSQL_select_Login_Admin(InsData _insData)
        {
            string sql = "select * from res.admin where a_id = " + _insData.PDic["a_id"];
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }
        private static string GetSQL_select_Login_Cus(InsData _insData)
        {
            string sql = "select * from res.customer where cus_id = " + _insData.PDic["cus_id"];
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }
        private static string GetSQL_insert_Sign_Cus(InsData _insData)
        {
            //性别默认为男，后续再改
            string sql = $"insert into res.customer(cus_id,cus_name,cus_phone) select {_insData.PDic["cus_id"]},'{_insData.PDic["cus_name"]}','{_insData.PDic["cus_phone"]}'" +
                $" where not exists(select * from res.customer where cus_id = {_insData.PDic["cus_id"]})";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_select_All_Inventory(InsData _insData)
        {
            string sql = "select * from res.inventory";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_update_Inventory(InsData _insData)
        {
            //首先判断i_mu_id是否存在(如果不存在会返回0)，然后修改对应的remain
            string sql = $"update res.inventory set i_remain = {_insData.PDic["i_remain"]} where i_mu_id = {_insData.PDic["i_mu_id"]} ";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_select_All_Menu(InsData _insData)
        {
            //获取menu的全部数据
            string sql = "select * from res.menu";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_insert_Menu(InsData _insData)
        {
            string sql = $"insert into res.menu(mu_id,mu_food,mu_description) select {_insData.PDic["mu_id"]},'{_insData.PDic["mu_food"]}','{_insData.PDic["mu_description"]}'" +
                $" where not exists(select * from res.menu where mu_id = {_insData.PDic["mu_id"]});";
            string sql2 = $"insert into res.inventory(i_mu_id,i_remain) select {_insData.PDic["mu_id"]},0" +
                $" where not exists(select * from res.inventory where i_mu_id = {_insData.PDic["mu_id"]})";
            
            Console.WriteLine($"生成SQL\"{sql + sql2}\"");
            return sql + sql2;
        }

        private static string GetSQL_delete_Menu(InsData _insData)
        {
            //首先判断mu_id是否存在，然后删除
            string sql = $"delete from res.menu where mu_id = {_insData.PDic["mu_id"]}";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_update_Menu(InsData _insData)
        {
            string sql = $"update res.menu set mu_food = '{_insData.PDic["mu_food"]}', mu_description = '{_insData.PDic["mu_description"]}'" +
                $" where mu_id = {_insData.PDic["mu_id"]} ";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_update_Size(InsData _insData)
        {
            string sql = $"update res.size set s_size = '{_insData.PDic["s_size"]}', s_price = {_insData.PDic["s_price"]}" +
                $" where s_id = {_insData.PDic["s_id"]} ";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_select_All_size(InsData _insData)
        {
            //获取size的全部数据
            string sql = "select * from res.size";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_insert_size(InsData _insData)
        {
            string sql = $"insert into res.size(s_id, s_mu_id,s_price,s_size) select {_insData.PDic["s_id"]},{_insData.PDic["s_mu_id"]},{_insData.PDic["s_price"]},'{_insData.PDic["s_size"]}'" +
                $" where not exists(select * from res.size where s_id = {_insData.PDic["s_id"]})";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_select_cusMenuView(InsData _insData)
        {
            //获取size的全部数据
            string sql = "select * from res.cus_menu_view";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

        private static string GetSQL_insert_Order(InsData _insData)
        {
            string sql = $"insert into res.order(o_id,o_cus_id,o_price,o_address) select {_insData.PDic["o_id"]},{_insData.PDic["o_cus_id"]},{_insData.PDic["o_price"]},'{_insData.PDic["o_address"]}'" +
                $" where not exists(select * from res.order where o_id = {_insData.PDic["o_id"]})";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }
        private static string GetSQL_insert_List(InsData _insData)
        {
            //特殊处理两个long的str
            string[] oIdAry = _insData.PDic["l_o_id"].Split("/");
            string[] sIdAry = _insData.PDic["l_s_id"].Split("/");
            string valueStr = "";
            if (oIdAry.Length == sIdAry.Length)  //确保长度一样
            {
                //valueStr的格式为"(v1,v2),(v1,v2)...(v1,v2);"
                for(int i = 0; i < oIdAry.Length - 1; i++)  //数组的最后一个是空的不要
                {
                    valueStr += $"({oIdAry[i]},{sIdAry[i]}),";
                }
                valueStr = valueStr.Substring(0, valueStr.Length - 1);   //去掉末尾,
                Console.WriteLine($"生成的valueStr为[{valueStr}]");
            }
            string sql = $"insert into res.list(l_o_id,l_s_id) values {valueStr};";
            Console.WriteLine($"生成SQL\"{sql}\"");
            return sql;
        }

    }
}
