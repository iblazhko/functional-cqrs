namespace CQRS.DTO;

public interface ICqrsDto;

public interface ICqrsCommandDto : ICqrsDto;

public interface ICqrsEventDto : ICqrsDto;

public interface IInventoryEventDto : ICqrsEventDto;

public interface IInventoryCommandDto : ICqrsCommandDto;

// By convention, concrete DTOs implementation records named with Command/Event suffix,
// while corresponding domain models do not have such suffix
