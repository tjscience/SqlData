using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Sql.TestEntities;
using System.Linq;

namespace Sql.Test
{
    [TestClass]
    public class ComprehensiveTest
    {
        #region Queries

        public const string CheckDBExists =
@"if exists(select name from dbo.sysdatabases where name = 'SqlDataTestDB') 
begin 
    select cast(1 as bit);
end
else 
    select cast(0 as bit);";
        public const string CreateDB = @"create database SqlDataTestDB";
        public const string CreateTables =
@"IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Product]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct] DROP CONSTRAINT [FK_OrderProduct_Product]

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Order]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct] DROP CONSTRAINT [FK_OrderProduct_Order]

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Order_User]') AND parent_object_id = OBJECT_ID(N'[dbo].[Order]'))
ALTER TABLE [dbo].[Order] DROP CONSTRAINT [FK_Order_User]

/****** Object:  Table [dbo].[User]    Script Date: 12/14/2015 3:10:07 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND type in (N'U'))
DROP TABLE [dbo].[User]

/****** Object:  Table [dbo].[Product]    Script Date: 12/14/2015 3:10:07 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND type in (N'U'))
DROP TABLE [dbo].[Product]

/****** Object:  Table [dbo].[OrderProduct]    Script Date: 12/14/2015 3:10:07 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderProduct]') AND type in (N'U'))
DROP TABLE [dbo].[OrderProduct]

/****** Object:  Table [dbo].[Order]    Script Date: 12/14/2015 3:10:07 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Order]') AND type in (N'U'))
DROP TABLE [dbo].[Order]

/****** Object:  Table [dbo].[Order]    Script Date: 12/14/2015 3:10:07 PM ******/
SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Order]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Order](
	[OrderId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Date] [datetime] NOT NULL,
 CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[OrderProduct]    Script Date: 12/14/2015 3:10:07 PM ******/
SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderProduct]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[OrderProduct](
	[OrderProductId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
 CONSTRAINT [PK_OrderProduct] PRIMARY KEY CLUSTERED 
(
	[OrderProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

/****** Object:  Table [dbo].[Product]    Script Date: 12/14/2015 3:10:07 PM ******/
SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

SET ANSI_PADDING ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Product]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Product](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED 
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_PADDING OFF

/****** Object:  Table [dbo].[User]    Script Date: 12/14/2015 3:10:07 PM ******/
SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

SET ANSI_PADDING ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[User](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [varchar](50) NOT NULL,
	[LastName] [varchar](50) NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_PADDING OFF

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Order_User]') AND parent_object_id = OBJECT_ID(N'[dbo].[Order]'))
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Order_User]') AND parent_object_id = OBJECT_ID(N'[dbo].[Order]'))
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_User]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Order]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct]  WITH CHECK ADD  CONSTRAINT [FK_OrderProduct_Order] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([OrderId])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Order]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct] CHECK CONSTRAINT [FK_OrderProduct_Order]

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Product]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct]  WITH CHECK ADD  CONSTRAINT [FK_OrderProduct_Product] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([ProductId])

IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderProduct_Product]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderProduct]'))
ALTER TABLE [dbo].[OrderProduct] CHECK CONSTRAINT [FK_OrderProduct_Product]";

        #endregion Queries

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            // Here we create the database if it does not exist.
            // We also clear out any data so that we can start the test with a fresh clean db.
            // This connection string should point to master of a MSSQL Server instance.
            var master = @"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;";
            Sql.Data.Store.AddConnection("master", master);
            // check if db exists
            var dbExists = Sql.Data.Store.Scalar<bool>(new Command { Connection = "master", Query = CheckDBExists });

            if (!dbExists)
            {
                // create the database.
                Sql.Data.Store.Query(new Command { Connection = "master", Query = CreateDB });
            }

            var sqlDataTestDB = @"Server=.\SQLEXPRESS;Database=SqlDataTestDB;Trusted_Connection=True;";
            Sql.Data.Store.AddConnection("testDB", sqlDataTestDB);

            // recreate tables
            Sql.Data.Store.Query(new Command { Connection = "testDB", Query = CreateTables });
        }

        public int TotalUserCount()
        {
            return Sql.Data.Store.Scalar<int>(new Command
            {
                Connection = "testDB",
                Query = "select count(*) from [User];"
            });
        }

        [TestMethod]
        public void CreateUsers()
        {
            // create the users list 
            var users = new List<User>();

            for (var i = 0; i < 1000; i++)
            {
                users.Add(new User
                {
                    FirstName = "First" + i,
                    LastName = "Last" + i
                });
            }

            users.CreateAll();
        }

        [TestMethod]
        public void GetAllUsers()
        {
            var users = Sql.Data.Store.All<User>().ToList();
            Assert.AreEqual(users.Count, 1000);
        }

        [TestMethod]
        public void QueryUsers()
        {
            var users = Sql.Data.Store.Query<User>(new Command
            {
                Query = "select * from [User];"
            }).ToList();
            Assert.AreEqual(users.Count, 1000);
        }

        [TestMethod]
        public void UpdateUser()
        {
            var command = new Command
            {
                Query = "select * from [User] where UserId = 99;"
            };
            var user99 = Sql.Data.Store.Scalar<User>(command);
            user99.FirstName = "Updated";
            user99.LastName = "Name";
            Assert.AreEqual(user99.FullName, "Updated Name");
            user99.Update();
            var updatedUser = Sql.Data.Store.Scalar<User>(command);
            Assert.AreEqual(updatedUser.FullName, "Updated Name");
        }

        [TestMethod]
        public void DeleteUserWithParameter()
        {
            int userId = 250;
            Sql.Data.Store.Query(new Command
            {
                Connection = "testDB",
                Query = "delete from [User] where UserId = @UserId;",
                Parameters = Command.AddParameters(Parameter.Create("UserId", userId))
            });
            Assert.AreEqual(TotalUserCount(), 999);
        }

        [TestMethod]
        public void DeleteUserEntity()
        {
            int userId = 350;
            var user = Sql.Data.Store.Scalar<User>(new Command
            {
                Query = "select * from [User] where UserId = @UserId;",
                Parameters = Command.AddParameters(Parameter.Create("UserId", userId))
            });
            user.Delete();
            Assert.AreEqual(TotalUserCount(), 998);
            Sql.Data.Store.
        }
    }
}
