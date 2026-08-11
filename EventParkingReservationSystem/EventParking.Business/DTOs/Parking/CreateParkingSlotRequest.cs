using System.ComponentModel.DataAnnotations;
using EventParking.Models.Enums;

namespace EventParking.Business.DTOs.Parking;

public class CreateParkingSlotRequest
{
    [Required(ErrorMessage = "Slot number is required.")]
    [StringLength(
        30,
        ErrorMessage = "Slot number cannot exceed 30 characters.")]
    public string SlotNumber { get; set; } = string.Empty;

    [StringLength(
        50,
        ErrorMessage = "Zone cannot exceed 50 characters.")]
    public string? Zone { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "9999999999999999.99",
        ErrorMessage = "Parking fee cannot be negative.")]
    public decimal? FeeOverride { get; set; }

    [EnumDataType(
        typeof(ParkingSlotStatus),
        ErrorMessage = "Invalid parking slot status.")]
    public ParkingSlotStatus Status { get; set; }
        = ParkingSlotStatus.Available;
}
