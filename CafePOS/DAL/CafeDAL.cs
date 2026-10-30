using System.Data;
using Microsoft.Data.SqlClient;
using CafePOS.DTO;
namespace CafePOS.DAL;
public sealed class CafeDAL
{
    public DataTable Tables()=>DatabaseHelper.Query("SELECT MaBan,TenBan,TrangThai FROM dbo.BanAn ORDER BY MaBan");
    public DataSet Menu()
    {
        using var c=DatabaseHelper.Open();using var adapter=new SqlDataAdapter("SELECT * FROM dbo.DanhMuc;SELECT * FROM dbo.MonAn;",c);
        var data=new DataSet();adapter.Fill(data);data.Tables[0].TableName="DanhMuc";data.Tables[1].TableName="MonAn";return data;
    }
    public DataTable Bill(int table)=>DatabaseHelper.Query("SELECT h.MaHoaDon,m.TenMon,d.SoLuong,d.DonGia,CAST(d.SoLuong*d.DonGia AS decimal(18,2)) AS ThanhTien FROM dbo.HoaDon h JOIN dbo.ChiTietHoaDon d ON d.MaHoaDon=h.MaHoaDon JOIN dbo.MonAn m ON m.MaMon=d.MaMon WHERE h.MaBan=@ban AND h.TrangThai=0",DatabaseHelper.P("@ban",SqlDbType.Int,table));
    private static SqlCommand Cmd(SqlConnection c,SqlTransaction t,string sql,params SqlParameter[] p){var q=new SqlCommand(sql,c,t);q.Parameters.AddRange(p);return q;}
    public int Order(OrderDTO order)
    {
        using var c=DatabaseHelper.Open();using var t=c.BeginTransaction();
        try
        {
            using(var check=Cmd(c,t,"SELECT TrangThai FROM dbo.BanAn WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@ban",DatabaseHelper.P("@ban",SqlDbType.Int,order.MaBan)))
                if(check.ExecuteScalar() is null)throw new InvalidOperationException("Không tìm thấy bàn.");
            decimal price;
            using(var q=Cmd(c,t,"SELECT DonGia FROM dbo.MonAn WITH(HOLDLOCK) WHERE MaMon=@mon",DatabaseHelper.P("@mon",SqlDbType.Int,order.MaMon)))price=q.ExecuteScalar() is decimal amount?amount:throw new InvalidOperationException("Món không còn tồn tại.");
            int id;
            using(var q=Cmd(c,t,"SELECT MaHoaDon FROM dbo.HoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@ban AND TrangThai=0",DatabaseHelper.P("@ban",SqlDbType.Int,order.MaBan)))
            {
                var found=q.ExecuteScalar();
                if(found is not null)id=(int)found;
                else{using var insert=Cmd(c,t,"INSERT dbo.HoaDon(MaBan) OUTPUT INSERTED.MaHoaDon VALUES(@ban)",DatabaseHelper.P("@ban",SqlDbType.Int,order.MaBan));id=(int)insert.ExecuteScalar()!;}
            }
            using(var q=Cmd(c,t,"IF EXISTS(SELECT 1 FROM dbo.ChiTietHoaDon WHERE MaHoaDon=@id AND MaMon=@mon) UPDATE dbo.ChiTietHoaDon SET SoLuong=SoLuong+@sl WHERE MaHoaDon=@id AND MaMon=@mon; ELSE INSERT dbo.ChiTietHoaDon(MaHoaDon,MaMon,SoLuong,DonGia) VALUES(@id,@mon,@sl,@gia); UPDATE dbo.BanAn SET TrangThai=N'Có khách' WHERE MaBan=@ban;",DatabaseHelper.P("@id",SqlDbType.Int,id),DatabaseHelper.P("@mon",SqlDbType.Int,order.MaMon),DatabaseHelper.P("@sl",SqlDbType.Int,order.SoLuong),DatabaseHelper.P("@gia",SqlDbType.Decimal,price),DatabaseHelper.P("@ban",SqlDbType.Int,order.MaBan)))q.ExecuteNonQuery();
            t.Commit();return id;
        }catch{t.Rollback();throw;}
    }
    public void Move(int from,int to)
    {
        using var c=DatabaseHelper.Open();using var t=c.BeginTransaction();
        try
        {
            using(var locks=Cmd(c,t,"SELECT MaBan FROM dbo.BanAn WITH(UPDLOCK,HOLDLOCK) WHERE MaBan IN(@a,@b) ORDER BY MaBan",DatabaseHelper.P("@a",SqlDbType.Int,from),DatabaseHelper.P("@b",SqlDbType.Int,to))) {using var r=locks.ExecuteReader();int count=0;while(r.Read())count++;if(count!=2)throw new InvalidOperationException("Bàn không tồn tại.");}
            using(var q=Cmd(c,t,"SELECT COUNT(*) FROM dbo.HoaDon WHERE MaBan=@ban AND TrangThai=0",DatabaseHelper.P("@ban",SqlDbType.Int,to)))if((int)q.ExecuteScalar()!>0)throw new InvalidOperationException("Bàn đích đang có khách.");
            using(var q=Cmd(c,t,"SELECT TrangThai FROM dbo.BanAn WHERE MaBan=@ban",DatabaseHelper.P("@ban",SqlDbType.Int,to)))if((string)q.ExecuteScalar()! !="Trống")throw new InvalidOperationException("Bàn đích không trống.");
            using(var q=Cmd(c,t,"UPDATE dbo.HoaDon SET MaBan=@to WHERE MaBan=@from AND TrangThai=0",DatabaseHelper.P("@to",SqlDbType.Int,to),DatabaseHelper.P("@from",SqlDbType.Int,from)))if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("Bàn nguồn chưa có hóa đơn đang mở.");
            using(var q=Cmd(c,t,"UPDATE dbo.BanAn SET TrangThai=CASE WHEN MaBan=@from THEN N'Trống' ELSE N'Có khách' END WHERE MaBan IN(@from,@to)",DatabaseHelper.P("@from",SqlDbType.Int,from),DatabaseHelper.P("@to",SqlDbType.Int,to)))q.ExecuteNonQuery();
            t.Commit();
        }catch{t.Rollback();throw;}
    }
    public PaymentDTO Pay(int table)
    {
        using var c=DatabaseHelper.Open();using var t=c.BeginTransaction();
        try
        {
            using(var q=Cmd(c,t,"SELECT MaBan FROM dbo.BanAn WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@ban",DatabaseHelper.P("@ban",SqlDbType.Int,table)))if(q.ExecuteScalar() is null)throw new InvalidOperationException("Bàn không tồn tại.");
            int id;
            using(var q=Cmd(c,t,"SELECT MaHoaDon FROM dbo.HoaDon WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@ban AND TrangThai=0",DatabaseHelper.P("@ban",SqlDbType.Int,table)))id=q.ExecuteScalar() is int found?found:throw new InvalidOperationException("Bàn chưa có hóa đơn để thanh toán.");
            decimal total;
            using(var q=Cmd(c,t,"SELECT COALESCE(SUM(SoLuong*DonGia),0) FROM dbo.ChiTietHoaDon WHERE MaHoaDon=@id",DatabaseHelper.P("@id",SqlDbType.Int,id)))total=(decimal)q.ExecuteScalar()!;
            using(var q=Cmd(c,t,"UPDATE dbo.HoaDon SET GioRa=GETDATE(),TongTien=@total,TrangThai=1 OUTPUT INSERTED.GioRa WHERE MaHoaDon=@id; UPDATE dbo.BanAn SET TrangThai=N'Trống' WHERE MaBan=@ban;",DatabaseHelper.P("@total",SqlDbType.Decimal,total),DatabaseHelper.P("@id",SqlDbType.Int,id),DatabaseHelper.P("@ban",SqlDbType.Int,table)))
            {var end=(DateTime)q.ExecuteScalar()!;t.Commit();return new PaymentDTO(id,table,end,total);}
        }catch{t.Rollback();throw;}
    }
    public DataTable Report()=>DatabaseHelper.Query("SELECT MaHoaDon,MaBan,GioVao,GioRa,TongTien FROM dbo.HoaDon WHERE TrangThai=1 ORDER BY MaHoaDon DESC");
}
