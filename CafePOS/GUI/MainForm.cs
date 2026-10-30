using System.Data;
using Microsoft.Data.SqlClient;
using CafePOS.BUS;
using CafePOS.DTO;
namespace CafePOS.GUI;
public partial class MainForm : Form
{
    private readonly CafeBUS service=new();private DataSet? menu;private int selectedTable;
    public MainForm(){InitializeComponent();Shown+=(_,_)=>Guard(Reload);}
    private void Guard(Action action){try{action();}catch(Exception ex) when(ex is SqlException or ArgumentException or InvalidOperationException or OverflowException){MessageBox.Show(this,ex is SqlException?"Không thể thao tác CSDL. Kiểm tra kết nối, dữ liệu và chạy Database/ScriptDB.sql.":ex.Message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    private void Reload()
    {
        menu=service.Menu();categories.DisplayMember="TenDanhMuc";categories.ValueMember="MaDanhMuc";categories.DataSource=menu.Tables["DanhMuc"];FilterMenu();
        var data=service.Tables();tables.Controls.Clear();dest.DisplayMember="TenBan";dest.ValueMember="MaBan";dest.DataSource=data.Copy();
        foreach(DataRow row in data.Rows)
        {
            var id=(int)row["MaBan"];var button=new Button{Text=row["TenBan"]+"\n"+row["TrangThai"],Width=125,Height=75,BackColor=(string)row["TrangThai"]=="Trống"?Color.SeaGreen:Color.Firebrick,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
            button.Click+=(_,_)=>Guard(()=>SelectTable(id));tables.Controls.Add(button);
        }
        if(selectedTable>0)SelectTable(selectedTable);report.DataSource=service.Report();
    }
    private void SelectTable(int id){selectedTable=id;bill.DataSource=service.Bill(id);selected.Text="Bàn đang chọn: "+id;var dt=(DataTable)bill.DataSource;total.Text="Tổng hóa đơn: "+(dt.Rows.Count==0?0:dt.AsEnumerable().Sum(x=>x.Field<decimal>("ThanhTien"))).ToString("N2")+" đ";}
    private void FilterMenu(){if(menu is null || categories.SelectedValue is not int category)return;var view=new DataView(menu.Tables["MonAn"]!){RowFilter="MaDanhMuc="+category};foods.DisplayMember="TenMon";foods.ValueMember="MaMon";foods.DataSource=view;}
    private void CategoryChanged(object? s,EventArgs e)=>FilterMenu();
    private void AddOrder(object? s,EventArgs e)=>Guard(()=>{if(foods.SelectedValue is not int food)throw new ArgumentException("Chọn món.");service.Order(new OrderDTO(selectedTable,food,(int)quantity.Value));Reload();});
    private void MoveTable(object? s,EventArgs e)=>Guard(()=>{if(dest.SelectedValue is not int target)throw new ArgumentException("Chọn bàn đích.");service.Move(selectedTable,target);selectedTable=target;Reload();});
    private void Pay(object? s,EventArgs e)=>Guard(()=>{if(selectedTable<=0)throw new ArgumentException("Vui lòng chọn bàn.");if(MessageBox.Show(this,total.Text+"\nXác nhận thanh toán?","Thanh toán",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;var receipt=service.Pay(selectedTable);Reload();MessageBox.Show(this,$"Hóa đơn {receipt.MaHoaDon}\nBàn {receipt.MaBan}\nTổng tiền: {receipt.TongTien:N2} đ\nĐã thanh toán.","Hóa đơn");});
    private void Catalog(object? s,EventArgs e){using var form=new CatalogForm();form.ShowDialog(this);Guard(Reload);}
    private void RefreshAll(object? s,EventArgs e)=>Guard(Reload);
}
