namespace Shared.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
    
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
    
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
    
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
    
    public class ExternalDependencyException : Exception
    {
        public ExternalDependencyException(string message) : base(message) { }
    }
}
