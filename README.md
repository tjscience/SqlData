#SQL Data
### Available on nuget https://www.nuget.org/packages/SqlData/
SQL Data is an ORM for C# to SQL without any of the bloat. If you have used ORMs in the past like Entity Framework or NHibernate, you may have noticed that they are ridiculously huge. The reason is that they have much more, sometimes unnecessary, functionality included. If you are using one of these ORMs and are utilizing all of this functionality, then SQL Data is not a good fit. Sql Data is designed to be extremely light weight. The number one goal of this ORM is to allow the developer to obtain data from a SQL database and use this data in strongly typed classes, and do it fast! To put it simply, SQL Data allows you to get data from a SQL database easily, quickly and efficiently.

####Here are some of the features included in SQL Data:

* Manages SQL connections in an intuitive manner
* Execute statements directly against the database without returning results
* Query and return a collection CLR classes
* Query and return dynamic results
* Query and return a scalar result
* Handle CRUD operations through intuitive extension methods on your classes
* You can do bulk inserts of any IList<> with one line of code!
* and much more...

####Here are some of the things that SQL Data does NOT do:

* Generate classes based on database schema
* Handle concurrency
* Slow you down!

##Basic Usage

Adding connections:

```c# 
Sql.Data.Store.AddConnection("SalesDB". salesDbConnectionStr);
```

Creating commands:

```c#
// Commands describe what statements to execute against the database.
var cmd = new Command
{
    Connection = "SalesDB",
    Query = "select * from [order] where amount > @MaxAmount;",
    Parameters = Command.AddParmaters(Parameter.Create("MaxAmount", 100))
};
```

Executing SQL statements without return values:

```c#
// You can execute sql statements against a database without concern for what data is returned by using the Query() method.
Sql.Data.Store.Query(cmd);
```

Quering for results:

```c#
// You can execute sql statements against a database and return results of a prticular type.
// Here, `orders` will be of the type IEnumerable<Order>.
// Note that this loads lazily, so unless you iterate over the orders, nothing will be fetched.
var orders = Sql.Data.Store.Query<Order>(cmd);
```

Others ways to query the database:

```c#
// There are several other ways to query the database.
// If you just want to return one result, use Scalar<>.
var order = Sql.Data.Store.Scalar<Order>(cmd);
var orderCount = Sql.Data.Store.Scalar<int>(cmd);

// One of the great things about SqlData is that it does not force you to have a type defined in order to return a list
// of entities. You can simply fetch dynamic results.

// This will return an IEnumerable<dynamic>.
var orders = Sql.Data.Store.QueryDynamic(cmd);

// Similarly, if you wish to only return a scalar...
// This returns a dynamic type.
var order = Sql.Data.Store.ScalarDynamic(cmd);

// We also allow you to fetch multiple results from a query. For example, let's say that you wanted
// to fetch sales orders, items, and price info all into the same result set. No problem, just use
// QueryMultiple().
var resultset = Sql.Data.Store.QueryMultiple(cmd);
```
