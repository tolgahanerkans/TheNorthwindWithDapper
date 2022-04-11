using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Northwind.Entities;

namespace Northwind.DataAccess
{
    public class OrderRepository
    {
        public IEnumerable<Order> GetAll()
        {
            var order = Enumerable.Empty<Order>();
            using (var connection = DBConnectionFactory.Create())
            {
                return connection.Query<Order>("Select * from Orders");
            }
        }

        public IEnumerable<Shipper> GetAllShippers()
        {
            var shipper = Enumerable.Empty<Shipper>();

            using (var connection = DBConnectionFactory.Create())
            {
                return connection.Query<Shipper>("select * from Shippers");
            }
        }

        public Order Find(int orderId)
        {
            using (var connection = DBConnectionFactory.Create())
            {
                const string orderSqlText = "select * from [Orders] where OrderID = @orderId";
                const string detailSqlText = @"
select
    od.*,
    NULL as Seperator,
    p.*
from 
[Order Details]  od
inner join Products p on od.ProductID = p.ProductID
where OrderID = @orderId";

                var parameters = new { orderId = orderId };
                var order = connection.QuerySingleOrDefault<Order>(
                    orderSqlText,
                    parameters);

                if (order != null)
                {
                    var orderDetails = connection.Query<OrderDetail, Product, OrderDetail>(
                        detailSqlText,
                        (detail, product) =>
                        {
                            detail.Product = product;
                            return detail;
                        },
                        // MapQueryResult,
                        parameters,
                        splitOn: "Seperator");

                    foreach (var detail in orderDetails)
                    {
                        order.OrderDetails.Add(detail);
                    }
                }

                return order;
            }
        }

        public void Add(Order order)
        {
            using (var connection = DBConnectionFactory.Create())
            {
                const string orderInsertSql = @"
insert into Orders (CustomerID,
EmployeeID,
OrderDate,
RequiredDate,
ShippedDate,
ShipVia,
ShipName,
Freight,
ShipAddress,
ShipCity,
ShipRegion,
ShipPostalCode,
ShipCountry)
values (@CustomerID,
@EmployeeID,
@OrderDate,
@RequiredDate,
@ShippedDate,
@ShipVia,
@ShipName,
@Freight,
@ShipAddress,
@ShipCity,
@ShipRegion,
@ShipPostalCode,
@ShipCountry)

select @@IDENTITY;";

                // Execute and Get OrderID!

                order.OrderID = connection.ExecuteScalar<int>(orderInsertSql, order);


                const string orderDetailInsertSql = @"
insert into [Order Details]
(OrderID,
ProductID,
UnitPrice,
Quantity,
Discount)
values (@OrderID, @ProductID, @UnitPrice, @Quantity, @Discount)";

                foreach (var orderDetail in order.OrderDetails)
                {
                    orderDetail.OrderID = order.OrderID;

                    connection.Execute(orderDetailInsertSql, orderDetail);
                }
            }
        }

        public void Remove(Order order)
        {
            throw new NotImplementedException();
        }

        public void Update(Order order)
        {
            const string updateOrderSql = @"
update Orders
set
    CustomerID = @CustomerID,
    EmployeeID = @EmployeeID,
    OrderDate = @OrderDate,
    RequiredDate = @RequiredDate,
    ShippedDate = @ShippedDate,
    ShipVia = @ShipVia,
    ShipName = @ShipName,
    Freight = @Freight,
    ShipAddress = @ShipAddress,
    ShipCity = @ShipCity,
    ShipRegion = @ShipRegion,
    ShipPostalCode = @ShipPostalCode,
    ShipCountry = @ShipCountry
where OrderID = @OrderID";

            using (var connection = DBConnectionFactory.Create())
            {
                var affectedRows = connection.Execute(updateOrderSql, order);

                if (affectedRows > 0)
                {
                    DeleteOrderDetailsByOrderId(connection, order.OrderID);

                    const string orderDetailInsertSql = @"
insert into [Order Details]
(OrderID,
ProductID,
UnitPrice,
Quantity,
Discount)
values (@OrderID, @ProductID, @UnitPrice, @Quantity, @Discount)";

                    foreach (var orderDetail in order.OrderDetails)
                    {
                        orderDetail.OrderID = order.OrderID;

                        connection.Execute(orderDetailInsertSql, orderDetail);
                    }


                }
            }
        }

        private void DeleteOrderDetailsByOrderId(SqlConnection connection, int orderId)
        {
            const string deleteDetailsSql = "delete from [Order details] where OrderID = @orderId";

            connection.Execute(deleteDetailsSql, new { orderId = orderId });
        }

        private OrderDetail MapQueryResult(OrderDetail orderDetail, Product product)
        {
            orderDetail.Product = product;
            return orderDetail;
        }
    }
}
