using AutoMapper;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Models.Dto.UtilityType;
using UtilityBillingSystem.Models.Dto.Tariff;
using UtilityBillingSystem.Models.Dto.BillingCycle;
using UtilityBillingSystem.Models.Dto.UtilityRequest;
using UtilityBillingSystem.Models.Dto.Bill;
using UtilityBillingSystem.Models.Dto.MeterReading;
using UtilityBillingSystem.Models.Dto.Notification;
using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Connection mappings
            CreateMap<Connection, ConnectionDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.UtilityTypeName, opt => opt.MapFrom(src => src.UtilityType != null ? src.UtilityType.Name : null))
                .ForMember(dest => dest.TariffName, opt => opt.MapFrom(src => src.Tariff != null ? src.Tariff.Name : null));

            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName));

            // UtilityType mappings
            CreateMap<UtilityType, UtilityTypeDto>();

            // Tariff mappings
            CreateMap<Tariff, TariffDto>();

            // BillingCycle mappings
            CreateMap<BillingCycle, BillingCycleDto>();

            // UtilityRequest mappings
            CreateMap<UtilityRequest, UtilityRequestDto>();

            // Payment mappings
            CreateMap<Payment, PaymentDto>();

            CreateMap<Payment, PaymentHistoryDto>()
                .ForMember(dest => dest.BillingPeriod, opt => opt.MapFrom(src => src.Bill != null ? src.Bill.BillingPeriod : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null && src.Bill.Connection.UtilityType != null 
                        ? src.Bill.Connection.UtilityType.Name 
                        : string.Empty));

            // Payment to RecentPaymentDto
            CreateMap<Payment, RecentPaymentDto>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
                .ForMember(dest => dest.ConsumerName, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null && src.Bill.Connection.User != null 
                        ? src.Bill.Connection.User.FullName 
                        : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null && src.Bill.Connection.UtilityType != null 
                        ? src.Bill.Connection.UtilityType.Name 
                        : string.Empty))
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.PaymentMethod));

            // Payment to PaymentAuditDto
            CreateMap<Payment, PaymentAuditDto>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
                .ForMember(dest => dest.ConsumerId, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null 
                        ? src.Bill.Connection.UserId 
                        : string.Empty))
                .ForMember(dest => dest.ConsumerName, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null && src.Bill.Connection.User != null 
                        ? src.Bill.Connection.User.FullName 
                        : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Bill != null && src.Bill.Connection != null && src.Bill.Connection.UtilityType != null 
                        ? src.Bill.Connection.UtilityType.Name 
                        : string.Empty))
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.PaymentMethod))
                .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.ReceiptNumber) 
                        ? src.ReceiptNumber 
                        : (!string.IsNullOrEmpty(src.UpiId) ? src.UpiId : "N/A")));

            // Bill mappings
            CreateMap<Bill, BillDto>();

            // Bill to OutstandingBillDto
            CreateMap<Bill, OutstandingBillDto>()
                .ForMember(dest => dest.BillId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ConsumerId, opt => opt.MapFrom(src => 
                    src.Connection != null ? src.Connection.UserId : string.Empty))
                .ForMember(dest => dest.ConsumerName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.User != null 
                        ? src.Connection.User.FullName 
                        : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.UtilityType != null 
                        ? src.Connection.UtilityType.Name 
                        : string.Empty))
                .ForMember(dest => dest.BillMonth, opt => opt.MapFrom(src => src.BillingPeriod))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount));

            // Bill to OverdueBillDto
            CreateMap<Bill, OverdueBillDto>()
                .ForMember(dest => dest.BillId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ConsumerName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.User != null 
                        ? src.Connection.User.FullName 
                        : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.UtilityType != null 
                        ? src.Connection.UtilityType.Name 
                        : string.Empty))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TotalAmount));

            // MeterReading to MeterReadingResponseDto
            CreateMap<MeterReading, MeterReadingResponseDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => 
                    src.Connection != null ? src.Connection.UserId : string.Empty))
                .ForMember(dest => dest.ConsumerName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.User != null 
                        ? src.Connection.User.FullName 
                        : string.Empty))
                .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => 
                    src.Connection != null && src.Connection.UtilityType != null 
                        ? src.Connection.UtilityType.Name 
                        : string.Empty))
                .ForMember(dest => dest.MeterNumber, opt => opt.MapFrom(src => 
                    src.Connection != null ? src.Connection.MeterNumber : string.Empty))
                .ForMember(dest => dest.Month, opt => opt.MapFrom(src => 
                    BillingCycleHelper.GetBillingPeriodLabel(src.ReadingDate)));
        }
    }
}

