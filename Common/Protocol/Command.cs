namespace Common.Protocol;

public enum Command
{
    Register = 1,
    Login = 2,
    Logout = 3,
    AddModel = 4,
    UpdateModel = 5,
    DeleteModel = 6,
    RequestReservation = 7,
    CancelReservation = 8,
    SearchVehicles = 9,
    GetVehicleDetails = 10,
    UploadImage = 11,
    DownloadImage = 12,
    CheckReservationStatus = 13,
    ActivityHistory = 14,
    ListMyVehicles = 15,
    ListVehiclesWithImage = 16
}