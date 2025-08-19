using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Services;
using ERP.API.Helpers;
using Dapper;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Exceptions;
using iText.IO.Exceptions;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using ClosedXML.Excel;
using System.Data;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesLeadController : ControllerBase
    {
        private readonly SalesLeadService _salesLeadService;
        private readonly SalesSummaryService _summaryService;

        public SalesLeadController(SalesLeadService salesLeadService, SalesSummaryService summaryService)
        {
            _salesLeadService = salesLeadService;
            _summaryService = summaryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesLead>>> GetAll()
        {
            var leads = await _salesLeadService.GetAllAsync();
            return Ok(leads);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesLead>> GetById(int id)
        {
            var lead = await _salesLeadService.GetByIdAsync(id);
            if (lead == null)
                return NotFound($"Lead with ID {id} not found");

            return Ok(lead);
        }

        /// <summary>
        /// Creates a new sales lead
        /// </summary>
        /// <param name="lead">The lead information in JSON format</param>
        /// <returns>The ID of the created lead</returns>
        /// <response code="201">Returns the ID of the created lead</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> Create([FromBody] SalesLead lead)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                // Validate required fields
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(lead.CustomerName))
                {
                    validationErrors.Add("Customer Name is required");
                }
                if (string.IsNullOrWhiteSpace(lead.LeadSource))
                {
                    validationErrors.Add("Lead Source is required");
                }

                var website = lead.Website?.Trim();
                if (!string.IsNullOrWhiteSpace(website))
                {
                    if (!website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                        !website.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && 
                        !website.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                    {
                        website = "http://" + website;
                    }
                    
                    if (!Uri.IsWellFormedUriString(website, UriKind.Absolute))
                    {
                        validationErrors.Add("The Website field must be a valid URL");
                    }
                    else
                    {
                        lead.Website = website;
                    }
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        statusCode = 400,
                        errors = validationErrors
                    });
                }                // Set default values
                lead.IsActive = lead.IsActive ?? true;
                lead.DateCreated = DateTime.UtcNow;
                lead.DateUpdated = DateTime.UtcNow;
                lead.Status = string.IsNullOrEmpty(lead.Status) ? "New" : lead.Status;                // Helper function to clean string values
                string CleanString(string? value)
                {
                    if (string.IsNullOrWhiteSpace(value) || value.Trim().ToLower() == "string")
                        return null;
                    return value.Trim();
                }

                // Sanitize input fields and convert empty strings and "string" to null
                lead.CustomerName = CleanString(lead.CustomerName) ?? string.Empty; // Required field
                lead.LeadSource = CleanString(lead.LeadSource);
                lead.ReferralSourceName = CleanString(lead.ReferralSourceName);
                lead.HospitalOfReferral = CleanString(lead.HospitalOfReferral);
                lead.DepartmentOfReferral = CleanString(lead.DepartmentOfReferral);
                lead.SocialMedia = CleanString(lead.SocialMedia);
                lead.QualificationStatus = CleanString(lead.QualificationStatus);
                lead.EventName = CleanString(lead.EventName);
                lead.Score = CleanString(lead.Score);
                lead.Comments = CleanString(lead.Comments?.Replace("//", ""));
                lead.LeadType = CleanString(lead.LeadType?.Replace("//", ""));
                lead.ContactName = CleanString(lead.ContactName?.Replace("//", ""));
                lead.Salutation = CleanString(lead.Salutation);
                lead.ContactMobileNo = CleanString(lead.ContactMobileNo);
                lead.LandLineNo = CleanString(lead.LandLineNo);
                lead.Email = CleanString(lead.Email);
                lead.Fax = CleanString(lead.Fax);
                lead.DoorNo = CleanString(lead.DoorNo);
                lead.Street = CleanString(lead.Street);
                lead.Landmark = CleanString(lead.Landmark);
                lead.Territory = CleanString(lead.Territory);
                lead.Area = CleanString(lead.Area);
                lead.City = CleanString(lead.City);
                lead.Pincode = CleanString(lead.Pincode);
                lead.District = CleanString(lead.District);
                lead.State = CleanString(lead.State);
                lead.LeadSource = lead.LeadSource?.Trim() ?? string.Empty;
                lead.Comments = lead.Comments?.Replace("//", "")?.Trim();
                lead.LeadType = lead.LeadType?.Replace("//", "")?.Trim();
                lead.ContactName = lead.ContactName?.Replace("//", "")?.Trim();
                
                // Handle territory as a string value
                lead.Territory = lead.Territory?.Trim();                
                  // Generate the lead ID first
                var leadId = await _salesLeadService.GenerateLeadIdAsync();
                lead.LeadId = leadId;

                // Create the lead with the generated ID
                var id = await _salesLeadService.CreateAsync(lead);

                // Create summary entry
                var summary = new SalesSummary
                {
                    Title = "Lead created",
                    Description = $"New lead {leadId} created for {lead.CustomerName}",
                    DateTime = DateTime.UtcNow,
                    Stage = "lead",
                    StageItemId = id.ToString(),
                    IsActive = true,
                    Entities = System.Text.Json.JsonSerializer.Serialize(new { LeadId = leadId, CustomerName = lead.CustomerName })
                };
                await _summaryService.CreateAsync(summary);

                // Return leadId in the requested format
                return Ok(new { id, leadId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the lead",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }        [HttpPut("{id}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalesLeadDto>> Update([FromRoute] int id, [FromBody] UpdateSalesLeadDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Get existing lead
                var lead = await _salesLeadService.GetByIdAsync(id);
                if (lead == null)
                {
                    return NotFound(new
                    {
                        message = $"Sales Lead with ID {id} not found",
                        statusCode = 404,
                        errors = new[] { $"Sales Lead with ID {id} not found" }
                    });
                }

                // Update database fields with new values if they are provided
                lead.UserCreated = dto.UserCreated ?? lead.UserCreated;
                lead.DateCreated = dto.DateCreated ?? lead.DateCreated;
                lead.UserUpdated = dto.UserUpdated ?? lead.UserUpdated;                lead.DateUpdated = DateTime.UtcNow;
                  // Helper function to clean string values
                string CleanString(string? value, string? existingValue = null, bool keepExisting = false)
                {
                    if (string.IsNullOrWhiteSpace(value) || value.Trim().ToLower() == "string")
                        return keepExisting ? existingValue : null;
                    return value.Trim();
                }

                // Update database fields with new values, converting empty strings and "string" to null
                lead.CustomerName = CleanString(dto.CustomerName, lead.CustomerName) ?? string.Empty; // Required field
                lead.LeadSource = CleanString(dto.LeadSource);
                lead.ReferralSourceName = CleanString(dto.ReferralSourceName);
                lead.HospitalOfReferral = CleanString(dto.HospitalOfReferral);
                lead.DepartmentOfReferral = CleanString(dto.DepartmentOfReferral);
                lead.SocialMedia = CleanString(dto.SocialMedia);
                lead.EventDate = dto.EventDate;
                lead.QualificationStatus = CleanString(dto.QualificationStatus);
                lead.EventName = CleanString(dto.EventName);
                lead.LeadId = CleanString(dto.LeadId, lead.LeadId, true);  // Keep existing LeadId if empty
                lead.Status = CleanString(dto.Status, lead.Status, true);   // Keep existing Status if empty
                lead.Score = CleanString(dto.Score);
                lead.IsActive = dto.IsActive;
                lead.Comments = CleanString(dto.Comments?.Replace("//", ""));
                lead.LeadType = CleanString(dto.LeadType?.Replace("//", ""));
                lead.ContactName = CleanString(dto.ContactName?.Replace("//", ""));
                lead.Salutation = CleanString(dto.Salutation);
                lead.ContactMobileNo = CleanString(dto.ContactMobileNo);
                lead.LandLineNo = CleanString(dto.LandLineNo);
                lead.Email = CleanString(dto.Email);
                lead.Fax = CleanString(dto.Fax);
                lead.DoorNo = CleanString(dto.DoorNo);
                lead.Street = CleanString(dto.Street);
                lead.Landmark = CleanString(dto.Landmark);
                lead.Website = CleanString(dto.Website);
                lead.Territory = CleanString(dto.Territory);
                lead.Area = CleanString(dto.Area);
                lead.City = CleanString(dto.City);
                lead.Pincode = CleanString(dto.Pincode);
                lead.District = CleanString(dto.District);
                lead.State = CleanString(dto.State);// Validate website URL format
                var website = lead.Website?.Trim();
                if (!string.IsNullOrWhiteSpace(website))
                {
                    if (!website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                        !website.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && 
                        !website.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                    {
                        website = "http://" + website;
                    }
                    
                    if (!Uri.IsWellFormedUriString(website, UriKind.Absolute))
                    {
                        return BadRequest(new
                        {
                            message = "Validation failed",
                            statusCode = 400,
                            errors = new[] { "The Website field must be a valid URL" }
                        });
                    }
                    
                    lead.Website = website;
                }

                // Validate door number
                if (!string.IsNullOrEmpty(lead.DoorNo) && lead.DoorNo.Length > 5)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        statusCode = 400,
                        errors = new[] { "Door number cannot exceed 5 characters" }
                    });
                }

                // Update the lead
                var success = await _salesLeadService.UpdateAsync(lead);
                if (!success)
                {
                    return StatusCode(500, new
                    {
                        message = $"Failed to update sales lead {id}",
                        statusCode = 500,
                        errors = new[] { "Database update operation failed" }
                    });
                }

                // Create summary entry for update
                var summary = new SalesSummary
                {
                    Title = $"Lead updated",
                    Description = $"Lead information updated for {lead.CustomerName}",
                    DateTime = DateTime.UtcNow,
                    Stage = "lead",
                    StageItemId = id.ToString(),
                    IsActive = true,
                    Entities = System.Text.Json.JsonSerializer.Serialize(new { LeadId = id, CustomerName = lead.CustomerName })
                };
                await _summaryService.CreateAsync(summary);

                // Return updated lead info as DTO
                return Ok(ConvertToDto(lead));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Failed to update sales lead {id}",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }        private SalesLeadDto ConvertToDto(SalesLead? lead)
        {
            if (lead == null)
            {
                throw new ArgumentNullException(nameof(lead), "Lead entity cannot be null");
            }                return new SalesLeadDto
            {
                Id = lead.Id ?? 0,
                UserCreated = lead.UserCreated ?? 0,
                DateCreated = lead.DateCreated,
                UserUpdated = lead.UserUpdated ?? 0,
                DateUpdated = lead.DateUpdated,
                CustomerName = lead.CustomerName ?? string.Empty,
                LeadSource = lead.LeadSource ?? string.Empty,
                ReferralSourceName = lead.ReferralSourceName ?? string.Empty,
                HospitalOfReferral = lead.HospitalOfReferral ?? string.Empty,
                DepartmentOfReferral = lead.DepartmentOfReferral ?? string.Empty,
                SocialMedia = lead.SocialMedia ?? string.Empty,
                EventDate = lead.EventDate,
                QualificationStatus = lead.QualificationStatus ?? string.Empty,
                EventName = lead.EventName ?? string.Empty,
                LeadId = lead.LeadId ?? string.Empty,
                Status = lead.Status ?? string.Empty,
                Score = lead.Score ?? string.Empty,
                IsActive = lead.IsActive ?? true,
                Comments = lead.Comments ?? string.Empty,
                LeadType = lead.LeadType ?? string.Empty,
                Territory = lead.Territory ?? string.Empty,
                Area = lead.Area ?? string.Empty,
                City = lead.City ?? string.Empty,
                District = lead.District ?? string.Empty,
                State = lead.State ?? string.Empty,
                Pincode = lead.Pincode ?? string.Empty
            };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _salesLeadService.GetByIdAsync(id);            if (lead == null)
                return NotFound($"Lead with ID {id} not found");            // Call function to handle cascade deletion
            using var connection = _salesLeadService.CreateConnection();
            await connection.ExecuteAsync("SELECT sp_delete_lead_cascade(@p_lead_id)", new { p_lead_id = id });

            // Create summary entry for deletion
            var summary = new SalesSummary
            {
                Title = $"Lead deleted - {lead.CustomerName}",
                Description = $"Lead deleted for {lead.CustomerName}",
                DateTime = DateTime.UtcNow,
                Stage = "lead",
                StageItemId = id.ToString(),
                IsActive = true,
                Entities = System.Text.Json.JsonSerializer.Serialize(new { LeadId = id, CustomerName = lead.CustomerName })
            };
            await _summaryService.CreateAsync(summary);

            return NoContent();
        }

        [HttpPost("grid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<(IEnumerable<SalesLeadGridResult> Results, int TotalRecords)>> GetSalesLeadsGrid([FromBody] SalesLeadGridRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request parameters",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _salesLeadService.GetSalesLeadsGridAsync(
                    request.SearchText,
                    request.Zones,
                    request.CustomerNames,
                    request.Territories,
                    request.Statuses,
                    request.Scores,
                    request.LeadTypes,
                    request.PageNumber,
                    request.PageSize,
                    request.OrderBy,
                    request.OrderDirection);

                // Ensure unique records in the grid API response
                var uniqueResults = result.Results.GroupBy(r => r.Id).Select(g => g.First()).ToList();
                return Ok(new { 
                    Results = uniqueResults, 
                    TotalRecords = result.TotalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving leads",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }
        [HttpPost("dropdown")]
        public async Task<ActionResult<(IEnumerable<LeadsDropdownResult> Results, int TotalRecords)>> GetLeadsDropdown([FromBody] LeadsDropdownRequest request)
        {
            try
            {
                if (request.PageNumber <= 0)
                {
                    return BadRequest("Page number must be greater than 0");
                }

                if (request.PageSize <= 0)
                {
                    return BadRequest("Page size must be greater than 0");
                }

                var result = await _salesLeadService.GetLeadsDropdownAsync(
                    request.SearchText,
                    request.PageNumber,
                    request.PageSize);

                return Ok(new { Results = result.Results, TotalRecords = result.TotalRecords });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("details/{id}")]
        public async Task<ActionResult<SalesLeadDetails>> GetLeadDetailsById(int id)
        {
            var leadDetails = await _salesLeadService.GetLeadDetailsByIdAsync(id);
            if (leadDetails == null)
                return NotFound($"Lead details with ID {id} not found");

            return Ok(leadDetails);
        }

        [HttpPost("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromBody] SalesLeadGridRequest request)
        {
            try
            {
                var result = await _salesLeadService.GetSalesLeadsGridAsync(
                    request.SearchText,
                    request.Zones,
                    request.CustomerNames,
                    request.Territories,
                    request.Statuses,                    request.Scores,
                    request.LeadTypes,
                    request.PageNumber,
                    request.PageSize,
                    request.OrderBy,
                    request.OrderDirection);

                // Filter by selected Lead IDs if provided
                if (request.SelectedLeadIds != null && request.SelectedLeadIds.Any())
                {
                    result.Results = result.Results.Where(r => r.LeadId != null && request.SelectedLeadIds.Contains(r.LeadId)).ToList();
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("SalesLeads");

                // Define headers
                var headers = new[] { "Lead ID", "Customer Name", "Lead Source", "Status", "Score", "Lead Type",
                    "Contact Name", "Contact Mobile", "Email", "Territory", "City", "State", "Date Created" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Populate data
                int row = 2;
                foreach (var lead in result.Results)
                {
                    worksheet.Cell(row, 1).Value = lead.LeadId ?? "";
                    worksheet.Cell(row, 2).Value = lead.CustomerName ?? "";
                    worksheet.Cell(row, 3).Value = lead.LeadSource ?? "";
                    worksheet.Cell(row, 4).Value = lead.Status ?? "";
                    worksheet.Cell(row, 5).Value = lead.Score ?? "";
                    worksheet.Cell(row, 6).Value = lead.LeadType ?? "";
                    worksheet.Cell(row, 7).Value = lead.ContactName ?? "";
                    worksheet.Cell(row, 8).Value = lead.ContactMobileNo ?? "";
                    worksheet.Cell(row, 9).Value = lead.Email ?? "";
                    worksheet.Cell(row, 10).Value = lead.TerritoryName ?? "";
                    worksheet.Cell(row, 11).Value = lead.CityName ?? "";
                    worksheet.Cell(row, 12).Value = lead.StateName ?? "";
                    worksheet.Cell(row, 13).Value = lead.DateCreated?.ToString("yyyy-MM-dd") ?? "";
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Convert to byte array
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"SalesLeads_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating Excel file: {ex.Message}");
            }
        }

        // New endpoint for PDF export
        [HttpPost("export/pdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] SalesLeadGridRequest request)
        {
            MemoryStream? stream = null;
            PdfWriter? writer = null;
            PdfDocument? pdf = null;
            Document? document = null;

            try
            {
                var result = await _salesLeadService.GetSalesLeadsGridAsync(
                    request.SearchText,
                    request.Zones,
                    request.CustomerNames,
                    request.Territories,
                    request.Statuses,
                    request.Scores,
                    request.LeadTypes,
                    request.PageNumber,
                    request.PageSize,
                    request.OrderBy,
                    request.OrderDirection);

                stream = new MemoryStream();

                var writerProperties = new WriterProperties()
                    .SetPdfVersion(PdfVersion.PDF_2_0);
                writer = new PdfWriter(stream, writerProperties);
                pdf = new PdfDocument(writer);
                document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                document.SetMargins(20, 20, 20, 20);

                var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                document.Add(new Paragraph("Sales Leads Report")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(normalFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(20));

                var table = new Table(new float[] { 1, 2, 1.5f, 1, 1, 1, 1.5f, 1.5f })
                    .UseAllAvailableWidth()
                    .SetFixedLayout();

                var headers = new[] { "Lead ID", "Customer Name", "Lead Source", "Status", "Score", "Lead Type", "Contact Name", "Contact Mobile" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(header).SetFont(boldFont))
                            .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetPadding(5)
                    );
                }

                foreach (var lead in result.Results)
                {
                    table.AddCell(new Cell().Add(new Paragraph(lead.LeadId ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.CustomerName ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.LeadSource ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.Status ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.Score ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.LeadType ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.ContactName ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(lead.ContactMobileNo ?? "").SetFont(normalFont)).SetPadding(5));
                }

                document.Add(table);

                int numberOfPages = pdf.GetNumberOfPages();
                for (int i = 1; i <= numberOfPages; i++)
                {
                    document.ShowTextAligned(
                        new Paragraph(String.Format("Page {0} of {1}", i, numberOfPages)).SetFont(normalFont),
                        559, 20, i, TextAlignment.RIGHT, VerticalAlignment.BOTTOM, 0);
                }

                document.Close();
                pdf.Close();
                writer.Close();

                stream.Position = 0;
                string fileName = $"SalesLeads_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF file: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                try
                {
                    document?.Close();
                    pdf?.Close();
                    writer?.Close();
                    stream?.Dispose();
                }
                catch
                {
                    // Suppress any errors during cleanup
                }
            }
        }

        [HttpPost("export/single-lead-pdf/{id}")]
        public async Task<IActionResult> ExportSingleLeadToPdf(int id)
        {
            MemoryStream? stream = null;
            PdfWriter? writer = null;
            PdfDocument? pdf = null;
            Document? document = null;

            try
            {
                var lead = await _salesLeadService.GetLeadDetailsByIdAsync(id);
                if (lead == null)
                    return NotFound($"Lead with ID {id} not found");

                stream = new MemoryStream();

                var writerProperties = new WriterProperties()
                    .SetPdfVersion(PdfVersion.PDF_2_0);

                writer = new PdfWriter(stream, writerProperties);
                pdf = new PdfDocument(writer);
                document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
                document.SetMargins(20, 20, 20, 20);

                var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                document.Add(new Paragraph($"Lead Details - {lead.LeadId ?? "N/A"}")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(normalFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(20));

                var detailsTable = new Table(2)
                    .UseAllAvailableWidth()
                    .SetFixedLayout();

                void AddDetailRow(string label, string? value)
                {
                    detailsTable.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont)).SetPadding(5));
                    detailsTable.AddCell(new Cell().Add(new Paragraph(value ?? "N/A").SetFont(normalFont)).SetPadding(5));
                }

                AddDetailRow("Lead ID", lead.LeadId);
                AddDetailRow("Customer Name", lead.CustomerName);
                AddDetailRow("Lead Source", lead.LeadSource);
                AddDetailRow("Status", lead.Status);
                AddDetailRow("Score", lead.Score);
                AddDetailRow("Lead Type", lead.LeadType);
                AddDetailRow("Contact Name", lead.ContactName);
                AddDetailRow("Contact Mobile", lead.ContactMobileNo);
                AddDetailRow("Email", lead.Email);
                AddDetailRow("Territory", lead.Territory);
                AddDetailRow("City", lead.City);
                AddDetailRow("State", lead.State);
                AddDetailRow("Date Created", lead.DateCreated?.ToString("yyyy-MM-dd HH:mm:ss"));

                document.Add(detailsTable);

                if (document != null)
                {
                    document.Close();
                }
                if (pdf != null)
                {
                    pdf.Close();
                }
                if (writer != null)
                {
                    writer.Close();
                }

                stream.Position = 0;
                string fileName = $"Lead_{lead.LeadId ?? id.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF file: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                try
                {
                    if (document != null)
                    {
                        document.Close();
                    }
                    if (pdf != null)
                    {
                        pdf.Close();
                    }
                    if (writer != null)
                    {
                        writer.Close();
                    }
                    stream?.Dispose();
                }
                catch
                {
                    // Suppress any errors during cleanup
                }
            }
        }

        [HttpPost("export/single-lead-pdf/by-lead-id/{leadId}")]
        public async Task<IActionResult> ExportSingleLeadToPdfByLeadId(string leadId)
        {
            try
            {
                var lead = await _salesLeadService.GetLeadDetailsByLeadIdAsync(leadId);
                if (lead == null)
                    return NotFound($"Lead with ID {leadId} not found");

                using (var memoryStream = new MemoryStream())
                {
                    using (var pdfDoc = new PdfDocument(new PdfWriter(memoryStream)))
                    {
                        var document = new Document(pdfDoc);
                        document.SetMargins(20, 20, 20, 20);

                        var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                        // Title - Lead Profile
                        document.Add(new Paragraph("LEAD PROFILE")
                            .SetFont(boldFont)
                            .SetFontSize(20)
                            .SetFontColor(new DeviceRgb(0, 51, 102))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(20));

                        // Add timestamp
                        document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            .SetFont(normalFont)
                            .SetFontSize(8)
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetMarginBottom(30));

                        // Basic Information Section
                        AddSectionHeader(document, "Basic Information", boldFont);
                        var basicInfoTable = new Table(2)
                            .UseAllAvailableWidth()
                            .SetWidth(UnitValue.CreatePercentValue(100));
                        
                        AddInfoRow(basicInfoTable, "Lead ID", lead.LeadId ?? "N/A", boldFont, normalFont);
                        AddInfoRow(basicInfoTable, "Customer Name", lead.CustomerName ?? "N/A", boldFont, normalFont);
                        AddInfoRow(basicInfoTable, "Lead Source", lead.LeadSource ?? "N/A", boldFont, normalFont);
                        AddInfoRow(basicInfoTable, "Lead Type", lead.LeadType ?? "N/A", boldFont, normalFont);
                        AddInfoRow(basicInfoTable, "Status", lead.Status ?? "N/A", boldFont, normalFont);
                        AddInfoRow(basicInfoTable, "Score", lead.Score ?? "N/A", boldFont, normalFont);
                        document.Add(basicInfoTable);

                        // Contact Details Section
                        AddSectionHeader(document, "Contact Details", boldFont);
                        var contactTable = new Table(2)
                            .UseAllAvailableWidth()
                            .SetWidth(UnitValue.CreatePercentValue(100));

                        AddInfoRow(contactTable, "Contact Name", lead.ContactName ?? "N/A", boldFont, normalFont);
                        AddInfoRow(contactTable, "Mobile Number", lead.ContactMobileNo ?? "N/A", boldFont, normalFont);
                        AddInfoRow(contactTable, "Email", lead.Email ?? "N/A", boldFont, normalFont);
                        document.Add(contactTable);

                        // Lead Details Section
                        AddSectionHeader(document, "Lead Details", boldFont);
                        var detailsTable = new Table(2)
                            .UseAllAvailableWidth()
                            .SetWidth(UnitValue.CreatePercentValue(100));

                        AddInfoRow(detailsTable, "Territory", lead.Territory ?? "Not Assigned", boldFont, normalFont);
                        AddInfoRow(detailsTable, "City", lead.City ?? "Not Assigned", boldFont, normalFont);
                        AddInfoRow(detailsTable, "State", lead.State ?? "Not Assigned", boldFont, normalFont);
                        AddInfoRow(detailsTable, "Date Created", lead.DateCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A", boldFont, normalFont);
                        document.Add(detailsTable);

                        // Notes & Interactions Section
                        AddSectionHeader(document, "Notes & Interactions", boldFont);
                        if (lead.BusinessChallenges != null && lead.BusinessChallenges.Any())
                        {
                            document.Add(new Paragraph("Business Challenges")
                                .SetFont(boldFont)
                                .SetFontSize(10)
                                .SetMarginTop(10)
                                .SetMarginBottom(5));

                            var challengesTable = new Table(2)
                                .UseAllAvailableWidth()
                                .SetWidth(UnitValue.CreatePercentValue(100));
                            
                            foreach (var challenge in lead.BusinessChallenges)
                            {
                                AddInfoRow(challengesTable, "Challenge", challenge.Challenges ?? "N/A", boldFont, normalFont);
                                AddInfoRow(challengesTable, "Solution", challenge.Solution ?? "N/A", boldFont, normalFont);
                            }
                            document.Add(challengesTable);
                        }

                        // Add page numbers
                        int pages = pdfDoc.GetNumberOfPages();
                        for (int i = 1; i <= pages; i++)
                        {
                            document.ShowTextAligned(
                                new Paragraph($"Page {i} of {pages}")
                                    .SetFont(normalFont)
                                    .SetFontSize(8),
                                PageSize.A4.GetWidth() - 40, 20, i,
                                TextAlignment.RIGHT, VerticalAlignment.BOTTOM, 0);
                        }

                        document.Close();
                    }

                    var pdfBytes = memoryStream.ToArray();
                    var fileName = $"Lead_{leadId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating PDF file: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
        }

        [HttpGet("cards")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeadCardsDto>> GetLeadCards()
        {
            try
            {
                var cards = await _salesLeadService.GetLeadCardsAsync();
                return Ok(cards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Failed to retrieve lead cards data",
                    error = ex.Message,
                    statusCode = 500 
                });
            }
        }

        /// <summary>
        /// Get lead by its ID (e.g., LD00001)
        /// </summary>
        /// <param name="leadId">The lead ID to search for (e.g., LD00001)</param>
        /// <returns>The lead details if found</returns>        /// <summary>
        /// Get a lead by its ID (e.g., LD00001)
        /// </summary>
        /// <param name="leadId">The lead ID to retrieve</param>
        /// <returns>The lead details</returns>
        [HttpGet("by-lead-id/{leadId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetByLeadId(string leadId)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(leadId))
                {
                    return BadRequest(new
                    {
                        message = "Lead ID cannot be empty",
                        statusCode = 400,
                        errors = new[] { "Lead ID cannot be empty" }
                    });
                }

                // Validate lead ID format
                if (!leadId.StartsWith("LD") || leadId.Length != 7)
                {
                    return BadRequest(new
                    {
                        message = "Invalid lead ID format. Expected format: LD00001",
                        statusCode = 400,
                        errors = new[] { "Invalid lead ID format. Expected format: LD00001" }
                    });
                }

                var lead = await _salesLeadService.GetByLeadIdAsync(leadId);
                if (lead == null)
                {
                    return NotFound(new
                    {
                        message = $"Lead with ID {leadId} not found",
                        statusCode = 404,
                        errors = new[] { $"Lead with ID {leadId} not found" }
                    });
                }

                // Return all fields that might be needed
                return Ok(new
                {
                    id = lead.Id,
                    userCreated = lead.UserCreated,
                    dateCreated = lead.DateCreated,
                    userUpdated = lead.UserUpdated,
                    dateUpdated = lead.DateUpdated,
                    customerName = lead.CustomerName,
                    leadSource = lead.LeadSource,
                    referralSourceName = lead.ReferralSourceName,
                    hospitalOfReferral = lead.HospitalOfReferral,
                    departmentOfReferral = lead.DepartmentOfReferral,
                    socialMedia = lead.SocialMedia,
                    eventDate = lead.EventDate,
                    qualificationStatus = lead.QualificationStatus,
                    eventName = lead.EventName,
                    leadId = lead.LeadId,
                    status = lead.Status,
                    score = lead.Score,
                    isActive = lead.IsActive,
                    comments = lead.Comments,
                    leadType = lead.LeadType,
                    contactName = lead.ContactName,
                    salutation = lead.Salutation,
                    contactMobileNo = lead.ContactMobileNo,
                    landLineNo = lead.LandLineNo,
                    email = lead.Email,
                    fax = lead.Fax,
                    doorNo = lead.DoorNo,
                    street = lead.Street,
                    landmark = lead.Landmark,
                    website = lead.Website,
                    territory = lead.Territory,
                    area = lead.Area,
                    city = lead.City,
                    district = lead.District,
                    state = lead.State,
                    pincode = lead.Pincode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving the lead",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }

        // Helper methods
        private void AddSectionHeader(Document document, string title, PdfFont boldFont)
        {
            document.Add(new Paragraph(title)
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(new DeviceRgb(0, 51, 102))
                .SetMarginTop(15)
                .SetMarginBottom(5)
                .SetBorderBottom(new SolidBorder(new DeviceRgb(200, 200, 200), 1)));
        }

        private void AddInfoRow(Table table, string label, string value, PdfFont boldFont, PdfFont normalFont, DeviceRgb? valueColor = null)
        {
            var bgColor = new DeviceRgb(245, 245, 245);
            
            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(boldFont))
                .SetPadding(5)
                .SetBackgroundColor(bgColor));

            var valueCell = new Cell()
                .Add(new Paragraph(value ?? "N/A")
                    .SetFont(normalFont)
                    .SetFontColor(valueColor ?? new DeviceRgb(0, 0, 0)));
            
            valueCell.SetPadding(5);
            table.AddCell(valueCell);
        }

        private DeviceRgb GetStatusColor(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return new DeviceRgb(0, 0, 0); // Black

            return status.ToLower() switch
            {
                "new" => new DeviceRgb(0, 123, 255),    // Blue
                "hot" => new DeviceRgb(220, 53, 69),     // Red
                "qualified" => new DeviceRgb(40, 167, 69), // Green
                "closed" => new DeviceRgb(108, 117, 125), // Gray
                _ => new DeviceRgb(0, 0, 0)  // Default black
            };
        }
    }
}