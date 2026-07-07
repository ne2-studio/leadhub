using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Input;

/// <summary>
/// Use cases available to the authenticated administrator for managing forms.
/// </summary>
public interface IAdminForms
{
    Task<Result<FormDetails, Error>> CreateForm(CreateFormInput input);

    Task<Result<FormDetails, Error>> UpdateForm(UpdateFormInput input);

    Task<UnitResult<Error>> DeleteForm(DeleteFormInput input);

    Task<Result<IReadOnlyList<FormSummary>, Error>> ListForms();

    Task<Result<FormDetails, Error>> GetForm(GetFormInput input);
}
