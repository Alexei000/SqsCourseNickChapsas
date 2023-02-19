using Customers.Api.Contracts.Messages;
using Customers.Api.Domain;
using Customers.Api.Mapping;
using Customers.Api.Messaging;
using Customers.Api.Repositories;
using FluentValidation;
using FluentValidation.Results;

namespace Customers.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IGitHubService _gitHubService;
    private readonly ISqsMessenger _sqsMessenger;

    public CustomerService(ICustomerRepository customerRepository, 
        IGitHubService gitHubService, ISqsMessenger sqsMessenger)
    {
        _customerRepository = customerRepository;
        _gitHubService = gitHubService;
        _sqsMessenger = sqsMessenger;
    }

    public async Task<bool> CreateAsync(Customer customer)
    {
        var existingUser = await _customerRepository.GetAsync(customer.Id);
        if (existingUser is not null)
        {
            var message = $"A user with id {customer.Id} already exists";
            throw new ValidationException(message, GenerateValidationError(nameof(Customer), message));
        }

        var isValidGitHubUser = await _gitHubService.IsValidGitHubUser(customer.GitHubUsername);
        if (!isValidGitHubUser)
        {
            var message = $"There is no GitHub user with username {customer.GitHubUsername}";
            throw new ValidationException(message, GenerateValidationError(nameof(customer.GitHubUsername), message));
        }
        
        var customerDto = customer.ToCustomerDto();
        bool response = await _customerRepository.CreateAsync(customerDto);

        if (response)
            await _sqsMessenger.SendMessageAsync(customer.ToCustomerCreatedMessage(), CancellationToken.None);

        return response;
    }

    public async Task<Customer?> GetAsync(Guid id)
    {
        var customerDto = await _customerRepository.GetAsync(id);
        return customerDto?.ToCustomer();
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        var customerDtos = await _customerRepository.GetAllAsync();
        return customerDtos.Select(x => x.ToCustomer());
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        var customerDto = customer.ToCustomerDto();
        
        bool isValidGitHubUser = await _gitHubService.IsValidGitHubUser(customer.GitHubUsername);
        if (!isValidGitHubUser)
        {
            var message = $"There is no GitHub user with username {customer.GitHubUsername}";
            throw new ValidationException(message, GenerateValidationError(nameof(customer.GitHubUsername), message));
        }
        
        bool updated = await _customerRepository.UpdateAsync(customerDto);
        if (updated)
            await _sqsMessenger.SendMessageAsync(customer.ToCustomerCreatedUpdatedMessage(), CancellationToken.None);

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        bool deleted = await _customerRepository.DeleteAsync(id);
        if (deleted)
        {
            await _sqsMessenger.SendMessageAsync(new CustomerDeleted
            {
                Id = id
            }, CancellationToken.None);
        }

        return deleted;
    }

    private static ValidationFailure[] GenerateValidationError(string paramName, string message)
    {
        return new []
        {
            new ValidationFailure(paramName, message)
        };
    }
}
