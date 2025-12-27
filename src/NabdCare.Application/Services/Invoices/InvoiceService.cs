using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Application.Services.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository repository,
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<InvoiceService> logger)
    {
        _repository = repository;
        _clinicRepository = clinicRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequestDto request)
    {
        _logger.LogInformation("Generating Invoice for Clinic {ClinicId}, Subscription {SubId}", request.ClinicId, request.SubscriptionId);

        var clinic = await _clinicRepository.GetEntityByIdAsync(request.ClinicId);
        if (clinic == null) throw new InvalidOperationException($"Clinic {request.ClinicId} not found");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClinicId = request.ClinicId,
            SubscriptionId = request.SubscriptionId,
            InvoiceNumber = await _repository.GenerateNextInvoiceNumberAsync(),
            
            // ✅ 2025: Populate New Fields
            Currency = request.Currency, 
            IdempotencyKey = request.IdempotencyKey,
            
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Issued,
            Type = request.Type,
            
            // Snapshot Data
            BilledToName = clinic.Name,
            BilledToAddress = clinic.Address,
            BilledToTaxNumber = clinic.TaxNumber,
            TaxRate = request.TaxRate,
            
            CreatedBy = _userContext.GetCurrentUserId() ?? "System"
        };

        decimal subTotal = 0;
        foreach (var itemDto in request.Items)
        {
            var total = itemDto.Quantity * itemDto.UnitPrice;
            subTotal += total;

            invoice.Items.Add(new InvoiceItem
            {
                Description = itemDto.Description,
                Note = itemDto.Note,
                Type = itemDto.Type,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Total = total,
                PeriodStart = itemDto.PeriodStart,
                PeriodEnd = itemDto.PeriodEnd
            });
        }

        invoice.SubTotal = subTotal;
        invoice.TaxAmount = subTotal * request.TaxRate;
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
        invoice.PaidAmount = 0;

        await _repository.CreateAsync(invoice);
        return _mapper.Map<InvoiceDto>(invoice);
    }

    // ... (GetByIdAsync, GetPagedAsync, MarkAsPaidAsync, VoidInvoiceAsync remain exactly as you had them)
    // Just ensure GetPagedAsync passes the new Currency filter if added.
    
    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        if (!_tenantContext.IsSuperAdmin && invoice.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("Cannot view invoice from another clinic.");

        return _mapper.Map<InvoiceDto>(invoice);
    }

    public async Task<PaginatedResult<InvoiceDto>> GetPagedAsync(InvoiceListRequestDto request)
    {
        Func<IQueryable<Invoice>, IQueryable<Invoice>>? abacFilter = null;

        if (!_tenantContext.IsSuperAdmin)
        {
            abacFilter = q => q.Where(i => i.ClinicId == _tenantContext.ClinicId);
            request.ClinicId = _tenantContext.ClinicId;
        }

        var result = await _repository.GetPagedAsync(request, abacFilter);

        return new PaginatedResult<InvoiceDto>
        {
            Items = _mapper.Map<IEnumerable<InvoiceDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<InvoiceDto?> MarkAsPaidAsync(Guid id, decimal amount, DateTime paidDate)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        invoice.PaidAmount += amount;
        invoice.PaidDate = paidDate;

        if (invoice.PaidAmount >= invoice.TotalAmount)
            invoice.Status = InvoiceStatus.Paid;
        else
            invoice.Status = InvoiceStatus.PartiallyPaid;

        invoice.UpdatedBy = _userContext.GetCurrentUserId();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice);
        return _mapper.Map<InvoiceDto>(invoice);
    }

    public async Task<InvoiceDto?> VoidInvoiceAsync(Guid id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot void a paid invoice.");

        invoice.Status = InvoiceStatus.Void;
        invoice.UpdatedBy = _userContext.GetCurrentUserId();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice);
        return _mapper.Map<InvoiceDto>(invoice);
    }
}