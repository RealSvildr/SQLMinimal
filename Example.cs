using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;

public class Example {
    // Initializing Lib
    internal DBContext dBContext = new DBContext("Example\\SQLEXPRESS", "DataBase");

    public Example() {
        //Some Possible Variables to Change
        dBContext.timeOut = 50; // In seconds
        dBContext.bufferSize = 65000; // Amout of bytes send per package (used for streaming only)
    }


    //Executing a Command
    public void ExecuteCommand(int id, string newName) {
        string sqlCommand = @"
            UPDATE
                Example
            SET
                Name = @p1
            WHERE
                ID = @p0";

        dbContext.ExecuteSqlCommand(sqlCommand, id, newName);
    }

    //Getting From Server
    public List<Example> GetList() {
        string sqlCommand = @"
            SELECT ID, Name
            FROM Example
        ";

        return dBContext.SqlQuery<Example>(sqlCommand);
    }
    public Example GetItem(int id) {
        string sqlCommand = @"
            SELECT ID, Name
            FROM Example
            WHERE ID = @p0
        ";

        return dBContext.SqlQuery<Example>(sqlCommand, id).FirstOrDefault();
    }

    //File Stream
    public void FileStream(System.Web.HttpContext httpContext, long offset, int id) {
        string sqlCommand = @"
            SELECT
                B.File.PathName() AS PathName,
                B.Size AS Size
            FROM
                Example
            WHERE
                ID = @p0
        ";

        dBContext.FileStream(httpContext, offset, sqlCommand, id);
    }

    public class Example {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}


// Using FileStream
//Example example = GetItem(Convert.ToInt32(context.Request["id"]));
//string fileName = (example.Name == "" ? "noName.jpg" : archive.Name);

//context.Response.ContentType = archive.Type;
//context.Response.AddHeader("Content-Disposition", "inline; filename=" + fileName);
//archiveBLL.FileStream(context, example.ID);