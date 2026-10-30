using System.Data;
using CafePOS.DAL;
using CafePOS.DTO;
namespace CafePOS.BUS;
public sealed class CafeBUS
{
    private readonly CafeDAL dal=new();
    public DataTable Tables()=>dal.Tables();public DataSet Menu()=>dal.Menu();public DataTable Bill(int table)=>dal.Bill(table);public DataTable Report()=>dal.Report();
    public int Order(OrderDTO order){if(order.MaBan<=0||order.MaMon<=0||order.SoLuong<=0||order.SoLuong>1000)throw new ArgumentException("Chọn bàn, món và số lượng từ 1 đến 1000.");return dal.Order(order);}
    public void Move(int from,int to){if(from<=0||to<=0||from==to)throw new ArgumentException("Chọn hai bàn khác nhau.");dal.Move(from,to);}
    public PaymentDTO Pay(int table){if(table<=0)throw new ArgumentException("Vui lòng chọn bàn.");return dal.Pay(table);}
}
