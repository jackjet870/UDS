﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using UDS.Entity;
using System.Data.Common;
using System.Data;

namespace UDS.SubModule.bulletin
{
    public partial class bulletinAction : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string UserID = null == Response.Cookies["UserID"] ? "" : Response.Cookies["UserID"].Value;

            switch (Request.HttpMethod)
            {
                case "GET":
                    string page = Request.Params["page"];
                    string rows = Request.Params["rows"];
                    string type = Request.Params["type"];

                    if (string.IsNullOrEmpty(page))
                        page = "1";
                    if (string.IsNullOrEmpty(rows))
                        rows = "10";

                    int rowstart = (int.Parse(page) - 1) * int.Parse(rows) + 1;
                    int rowend = int.Parse(page) * int.Parse(rows);

                    string sqlTemplate = "select * from " +
                        "(select bulletinid, subject, contents, createtime, sendtime, " +
                            "(select count(*) from uds_bulletinreadlist t where t.bulletinid = bulletinid {2}) as readcount," +
                            "ROW_NUMBER() over (order by buletinid) as rowno " +
                            "from uds_bulletin) as A where rowno >= {0} and rowno <= {1} {3}";

                    string countTemplate = "select count(*) from uds_bulletin {0}";

                    string sql = "";
                    string countsql = "";

                    switch (type)
                    {
                        case "1":
                            //获取全部公告
                            sql = string.Format(sqlTemplate, rowstart, rowend, "", "");
                            countsql = string.Format(countTemplate, "");
                            break;
                        case "2":
                            //获取当前用户的全部未读公告
                            sql = string.Format(sqlTemplate, rowstart, rowend,
                                "and t.staffid = '" + UserID + "'", " and readcount = 0");
                            countsql = string.Format(countTemplate, 
                                "where (bulletinid not in (select bulletinid from uds_bulletinreadlist where staffid = '" + UserID + "'))");
                            break;
                        case "3":
                            sql = string.Format(sqlTemplate, rowstart, rowend,
                               "and t.staffid = '" + UserID + "'", "");
                            //获取当前用户的全部公告
                            countsql = string.Format(countTemplate, "");
                            break;
                    }

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["default"].ConnectionString))
                    {
                        SqlCommand comm = new SqlCommand(string.Format(sql, rowstart, rowend), con);
                        SqlDataReader reader = null;
                        try
                        {
                            con.Open();
                            reader = comm.ExecuteReader();
                            IList<UDSBulletin> bulletins = new List<UDSBulletin>();
                            while (reader.Read())
                            {
                                UDSBulletin buletting = new UDSBulletin()
                                {
                                    Bulletinid = reader.GetInt32(0),
                                    Subject = reader.GetString(1),
                                    Contents = reader.GetString(2),
                                    Createtime = reader.GetDateTime(3).ToString(),
                                    Sendtime = reader.GetDateTime(4).ToString(),
                                    Readcount = reader.GetInt32(5)
                                };

                                bulletins.Add(buletting);
                            }

                            reader.Close();
                            comm.CommandText = countsql;

                            int totalCount = int.Parse(comm.ExecuteScalar().ToString());

                            con.Close();

                            Records r = new Records();
                            r.Page = int.Parse(page);
                            r.Rows = int.Parse(rows);
                            r.Datas = bulletins;

                            var jsonSer = new Newtonsoft.Json.JsonSerializer();
                            StringWriter sw = new StringWriter();
                            using (JsonWriter jw = new JsonTextWriter(sw))
                            {
                                jw.Formatting = Formatting.Indented;

                                jsonSer.Serialize(jw, r);
                            }

                            Response.ContentType = "text/json";

                            Response.Write(sw.ToString());
                            sw.Close();
                            Response.End();
                        }
                        catch (Exception eX)
                        {
                            if (null != reader && !reader.IsClosed)
                                reader.Close();

                            if (System.Data.ConnectionState.Open == con.State)
                                con.Close();

                            Response.StatusCode = 400;
                            Response.ContentType = "text/json";
                            Response.Write(eX.Message);
                            Response.End();
                        }
                    }

                    break;
                case "POST":
                    string insertBulleting = "insert into UDS_Bulletin(subject, content, createtime, sendtime) values('{0}', '', getDate(), getDate());SELECT SCOPE_IDENTITY();";
                    string insertAttache = "insert into UDS_BulletinAttachment(bulletinid, attachmentname, attachmentpath) values('{0}', '{1}', '{2}')";

                    string title = Request.Params["t"];
                    string uuid = Request.Params["uuid"];

                    if (string.IsNullOrWhiteSpace(uuid))
                    {
                        Response.StatusCode = 400;
                        Response.Write("错误的公告发布，请刷新页面后重新尝试");
                        Response.End();
                    }
                    else
                    {
                        if (null == Session["CUploadUUID"])
                        {
                            Response.StatusCode = 400;
                            Response.Write("找不到匹配的公告发布通道");
                            Response.End();
                        }
                        else
                        {
                            if(uuid != Session["CUploadUUID"].ToString())
                            {
                                Response.StatusCode = 400;
                                Response.Write("公告发布上下文不一致，请刷新页面后重试");
                                Response.End();
                            }
                            else
                            {
                                if (null == Session[uuid])
                                {
                                    Response.StatusCode = 400;
                                    Response.Write("找不到匹配的公告发布内容");
                                    Response.End();
                                }
                                else
                                {
                                    List<string> attaches = Session[uuid] as List<string>;

                                    string exeSql = string.Format(insertBulleting, title);

                                    SqlTransaction trans = null;

                                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["default"].ConnectionString))
                                    {
                                        try
                                        {
                                            con.Open();
                                            trans = con.BeginTransaction();
                                            SqlCommand comm = con.CreateCommand();
                                            comm.CommandText = exeSql;
                                            comm.Transaction = trans;

                                            int id = int.Parse(comm.ExecuteScalar().ToString());

                                            foreach (string s in attaches)
                                            {
                                                exeSql = string.Format(insertAttache, id, s, uuid);
                                                comm.CommandText = exeSql;
                                                comm.ExecuteNonQuery();
                                            }

                                            trans.Commit();
                                            con.Close();

                                            Response.Write("公告发布成功");
                                            Response.End();
                                        }
                                        catch (Exception eX)
                                        {
                                            if (null != trans)
                                                trans.Rollback();

                                            con.Close();

                                            Response.StatusCode = 400;
                                            Response.Write(eX.Message);
                                            Response.End();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case "PUT":
                    break;
                case "HEAD":
                    break;
                case "DELETE":
                    break;
            }
        }
    }
}