using TakeServus.Application.DTOs.Jobs;

namespace TakeServus.Application.DTOs.Customers;

public class CustomerDetailResponse : CustomerResponse
{
  public List<JobResponse> Jobs { get; set; } = new();
}