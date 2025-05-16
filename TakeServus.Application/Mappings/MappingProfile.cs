using AutoMapper;
using TakeServus.Application.DTOs.Customers;
using TakeServus.Application.DTOs.Jobs.Feedback;
using TakeServus.Application.DTOs.Invoices;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.DTOs.Jobs.Activities;
using TakeServus.Application.DTOs.Jobs.Materials;
using TakeServus.Application.DTOs.Jobs.Notes;
using TakeServus.Application.DTOs.Jobs.Photos;
using TakeServus.Application.DTOs.Materials;
using TakeServus.Application.DTOs.Users;
using TakeServus.Domain.Entities;

namespace TakeServus.Application.Mappings;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    // Customers
    CreateMap<Customer, CustomerResponse>();
    CreateMap<Customer, CustomerDetailResponse>();
    CreateMap<CreateCustomerRequest, Customer>();
    CreateMap<UpdateCustomerRequest, Customer>();

    // Users
    CreateMap<User, UserResponse>();
    CreateMap<CreateUserRequest, User>();
    CreateMap<UpdateUserRequest, User>();

    // Materials
    CreateMap<Material, MaterialResponse>();
    CreateMap<CreateMaterialRequest, Material>();
    CreateMap<UpdateMaterialRequest, Material>();

    // Jobs
    CreateMap<Job, JobResponse>();
    CreateMap<CreateJobRequest, Job>();

    // Job Activities
    CreateMap<JobActivity, JobActivityResponse>()
        .ForMember(dest => dest.PerformedBy, opt => opt.MapFrom(src => src.PerformedByUser.FullName));
    CreateMap<CreateJobActivityRequest, JobActivity>();

    // Job Notes
    CreateMap<JobNote, JobNoteResponse>();
    CreateMap<CreateJobNoteRequest, JobNote>();
    CreateMap<UpdateJobNoteRequest, JobNote>();

    // Job Materials
    CreateMap<JobMaterial, JobMaterialResponse>()
        .ForMember(dest => dest.MaterialName, opt => opt.MapFrom(src => src.Material.Name));
    CreateMap<CreateJobMaterialRequest, JobMaterial>();
    CreateMap<UpdateJobMaterialRequest, JobMaterial>();

    // Job Photos
    CreateMap<JobPhoto, JobPhotoResponse>();

    // Invoices
    CreateMap<Invoice, InvoiceResponse>();
    CreateMap<CreateInvoiceRequest, Invoice>();

    // Feedback
    CreateMap<JobFeedback, JobFeedbackResponse>();
    CreateMap<CreateJobFeedbackRequest, JobFeedback>();
  }
}