using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
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
    private readonly IAccessPolicy<Invoice> _policy;

    public InvoiceService(
        IInvoiceRepository repository,
        IClinicRepository clinicRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<InvoiceService> logger,
        IAccessPolicy<Invoice> policy)
    {
        _repository = repository;
        _clinicRepository = clinicRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _mapper = mapper;
        _logger = logger;
        _policy = policy;
    }

    public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequestDto request)
    {
        _logger.LogInformation("Generating Invoice for Clinic {ClinicId}, Subscription {SubId}", request.ClinicId, request.SubscriptionId);

        var clinic = await _clinicRepository.GetEntityByIdAsync(request.ClinicId);
        if (clinic == null) throw new KeyNotFoundException($"Clinic {request.ClinicId} not found");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClinicId = request.ClinicId,
            SubscriptionId = request.SubscriptionId,
            InvoiceNumber = await _repository.GenerateNextInvoiceNumberAsync(),
            
            Currency = request.Currency, 
            IdempotencyKey = request.IdempotencyKey,
            
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Issued,
            Type = request.Type,
            
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

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        if (!await _policy.EvaluateAsync(_tenantContext, "read", invoice))
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

    public async Task<InvoiceDto?> VoidInvoiceAsync(Guid id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        if (!await _policy.EvaluateAsync(_tenantContext, "write", invoice))
            throw new UnauthorizedAccessException("Access denied to this invoice.");

        if (invoice.PaymentAllocations.Any())
        {
            throw new DomainException(ErrorCodes.INVOICE_ALREADY_PAID, "Cannot void an invoice that has payments. Please refund the payments first.");
        }

        invoice.Status = InvoiceStatus.Void;
        invoice.UpdatedBy = _userContext.GetCurrentUserId();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice);
        return _mapper.Map<InvoiceDto>(invoice);
    }

    public async Task<InvoiceDto?> WriteOffInvoiceAsync(Guid id, string reason)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice == null) return null;

        if (!await _policy.EvaluateAsync(_tenantContext, "write", invoice))
            throw new UnauthorizedAccessException("Access denied to this invoice.");

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Void)
        {
            throw new DomainException(ErrorCodes.INVALID_OPERATION, $"Cannot write off an invoice with status {invoice.Status}.");
        }

        invoice.Status = InvoiceStatus.Uncollectible;

        invoice.UpdatedBy = _userContext.GetCurrentUserId();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(invoice);
        return _mapper.Map<InvoiceDto>(invoice);
    }
}