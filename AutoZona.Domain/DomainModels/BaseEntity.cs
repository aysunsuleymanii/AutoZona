using System.ComponentModel.DataAnnotations;

namespace AutoZona.Domain.DomainModels;

public class BaseEntity
{
    [Key] public Guid Id { get; set; }
}