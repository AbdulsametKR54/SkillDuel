using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillDuel.Application.DTOs.Admin;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Application.Common;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkillDuel.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesController(
        ICategoryRepository categoryRepository, 
        IQuestionRepository questionRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Category>>>> GetAll()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Category>>.SuccessResult(categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CategoryCreateRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Slug = request.Slug
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        // Service-level check: Check if any questions reference this category
        // We'll use GetPagedAsync with pageSize 1 to check existence efficiently
        var (questions, total) = await _questionRepository.GetPagedAsync(id, null, null, 1, 1);
        if (total > 0)
        {
            return BadRequest("Bu kategoriye bağlı sorular olduğu için silinemez.");
        }

        await _categoryRepository.DeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}
