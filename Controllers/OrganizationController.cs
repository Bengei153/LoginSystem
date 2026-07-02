using LoginSystem.Data;
using LoginSystem.DTOs.Organizations;
using LoginSystem.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginSystem.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "SuperAdmin")]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrganizationsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(CreateOrganizationRequest request)
    {
        if (await _db.Organizations.AnyAsync(o => o.Slug == request.Slug))
            return BadRequest("An organization with this slug already exists.");

        var org = new Models.Organization
        {
            Name = request.Name,
            Slug = request.Slug,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Description = request.Description,
            CreatedBy = User.GetUserId()
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        return Ok(new OrganizationResponse(org.Id, org.Name, org.Slug, org.IsActive, org.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<List<OrganizationResponse>>> GetAll() =>
        Ok(await _db.Organizations
            .Select(o => new OrganizationResponse(o.Id, o.Name, o.Slug, o.IsActive, o.CreatedAt))
            .ToListAsync());

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org is null) return NotFound();
        org.IsActive = false; // BR8 — users stay, login just gets blocked (already enforced in Login)
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org is null) return NotFound();
        org.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<OrganizationResponse>> Update(Guid id, UpdateOrganizationRequest request)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org is null) return NotFound();

        if (request.Name is not null) org.Name = request.Name;
        if (request.Description is not null) org.Description = request.Description;
        if (request.LogoUrl is not null) org.LogoUrl = request.LogoUrl;
        if (request.Address is not null) org.Address = request.Address;
        if (request.ContactEmail is not null) org.ContactEmail = request.ContactEmail;
        if (request.ContactPhone is not null) org.ContactPhone = request.ContactPhone;

        await _db.SaveChangesAsync();
        return Ok(new OrganizationResponse(org.Id, org.Name, org.Slug, org.IsActive, org.CreatedAt));
    }
}