using System.Security.Claims;
using AutoMapper;
using HashidsNet;
using myfreelas.Dtos.Freela;
using myfreelas.Exceptions.BaseException;
using myfreelas.Extension;
using myfreelas.Pagination;
using myfreelas.Repositories.Customer;
using myfreelas.Repositories.Freela;
using myfreelas.Repositories.Installment;

namespace myfreelas.Services.Freela;

public class FreelaService : IFreelaService
{
    private readonly IFreelaRepository _freelaRpository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly IMapper _mapper;
    private readonly IHashids _hashids;

    public FreelaService(IFreelaRepository freelaRepository,
        ICustomerRepository customerRepository,
        IInstallmentRepository installmentRepository,
        IMapper mapper,
        IHashids hashids)
    {
        _freelaRpository = freelaRepository;
        _customerRepository = customerRepository;
        _installmentRepository = installmentRepository;
        _mapper = mapper;
        _hashids = hashids;
    }
    public async Task<ResponseFreelaJson> GetByIdAsync(
        ClaimsPrincipal logged, string fHashId)
    {
        var userId = GetCurrentUserId(logged);
        IsHash(fHashId);
        var freelaId = _hashids.DecodeSingle(fHashId);
        var freela = await _freelaRpository.GetByIdAsync(userId, freelaId);
        if (freela is null)
        {
            throw new ProjectNotFoundException("Projeto não encontrado");
        }
        return _mapper.Map<ResponseFreelaJson>(freela);
    }

    public async Task<List<ResponseAllFreelasJson>> GetAllAsync(ClaimsPrincipal logged,
    RequestGetFreelaJson request, PaginationParameters paginationParameters)
    {
        var userId = GetCurrentUserId(logged);
        var freelas = await _freelaRpository.GetAllAsync(userId, paginationParameters);
        var filters = Filter(request, freelas);
        return _mapper.Map<List<ResponseAllFreelasJson>>(filters);

    }

    public async Task<ResponseFreelaJson> RegisterFreelaAsync(ClaimsPrincipal logged,
        RequestRegisterFreelaJson request)
    {
        var userId = GetCurrentUserId(logged);
        var isHash = _hashids.TryDecodeSingle(request.CustomerId, out int number);
        if (!isHash)
        {
            throw new InvalidIDException("ID de cliente inválido");
        }
        var customerId = _hashids.DecodeSingle(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, userId);
        if (customer is null)
        {
            throw new CustomerNotFoundException("Cliente não encontrado");
        }
        var freela = _mapper.Map<Models.Freela>(request);
        freela.UserId = userId;

        var priceInstallment = freela.Price / freela.PaymentInstallment;
        var paymentMonth = freela.StartPayment;
        var finish = freela.PaymentInstallment;

        for (int i = 0; i < finish; i++)
        {
            var item = new Models.Installment();
            item.FreelaId = freela.Id;
            item.Month = paymentMonth.AddMonths(i);
            item.Value = priceInstallment;
            freela.Installments.Add(item);
        }

        await _freelaRpository.RegisterFreelaAsync(freela);
        return _mapper.Map<ResponseFreelaJson>(freela);
    }
    public async Task DeleteAsync(ClaimsPrincipal logged, string fHashId)
    {
        var userId = GetCurrentUserId(logged);
        IsHash(fHashId);
        var freelaId = _hashids.DecodeSingle(fHashId);
        var freela = await _freelaRpository.GetByIdAsync(userId, freelaId);
        if (freela is null)
        {
            throw new ProjectNotFoundException("Projeto não encontrado");
        }
        await _freelaRpository.DeleteAsync(freela);
    }
    public async Task UpdateAsync(ClaimsPrincipal logged, string fHashId,
        RequestUpdateFreelaJson request)
    {
        var userId = GetCurrentUserId(logged);
        IsHash(fHashId);
        var freelaId = _hashids.DecodeSingle(fHashId);
        var freela = await _freelaRpository.GetByIdUpdateAsync(userId, freelaId);
        if (freela is null)
        {
            throw new ProjectNotFoundException("Projeto não encontrado");
        }
        _mapper.Map(request, freela);
        await _freelaRpository.UpdateAsync();
    }

    private int GetCurrentUserId(ClaimsPrincipal logged)
    {
        return int.Parse(logged.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    private void IsHash(string hashid)
    {
        var isHash = _hashids.TryDecodeSingle(hashid, out int id);
        if (!isHash)
        {
            throw new InvalidIDException("ID do projeto inválido");
        }
    }
    private static List<Models.Freela> Filter(RequestGetFreelaJson request, List<Models.Freela> freelas)
    {
        var filters = freelas;
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            filters = freelas.Where(c => c.Name.CompareWithIgnoreCase(request.Name)).ToList();
        }
        return filters.OrderBy(c => c.Name).ToList();
    }

    // private async void AddInstallment(Models.Freela freela)
    // {

    // }
}
