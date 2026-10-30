namespace CafePOS.DTO;
public sealed record OrderDTO(int MaBan,int MaMon,int SoLuong);
public sealed record PaymentDTO(int MaHoaDon,int MaBan,DateTime GioRa,decimal TongTien);
