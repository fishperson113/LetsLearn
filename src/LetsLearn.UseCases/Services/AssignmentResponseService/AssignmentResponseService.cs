using LetsLearn.Core.Entities;
using LetsLearn.Core.Interfaces;
using LetsLearn.UseCases.DTOs;
using LetsLearn.UseCases.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.Services.AssignmentResponseService
{
    public class AssignmentResponseService : IAssignmentResponseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AssignmentResponseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private AssignmentResponseDTO ToDto(AssignmentResponse entity)
        {
            return new AssignmentResponseDTO
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                StudentId = entity.StudentId,
                Data = new AssignmentResponseData
                {
                    SubmittedAt = entity.SubmittedAt,
                    Files = entity.Files.ToList(),
                    Mark = entity.Mark,
                    Note = entity.Note
                }
            };
        }

        public async Task<AssignmentResponseDTO> GetByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.AssignmentResponses.GetByIdWithFilesAsync(id);
            if (entity == null)
            {
                throw new Exception("Assignment response not found"); 
            }
            return ToDto(entity);
        }

        public async Task<AssignmentResponseDTO> CreateAsync(CreateAssignmentResponseRequest dto, Guid studentId)
        {
            var entity = new AssignmentResponse
            {
                Id = Guid.NewGuid(),
                TopicId = dto.TopicId,
                StudentId = studentId,
                SubmittedAt = dto.Data.SubmittedAt,
                Note = dto.Data.Note,
                Mark = dto.Data.Mark,
                Files = new List<CloudinaryFile>()
            };

            var files = dto.Data.Files;
            foreach (var file in files)
            {
                file.Id = Guid.NewGuid();
                file.AssignmentResponseId = entity.Id;
                entity.Files.Add(file);
            }

            await _unitOfWork.AssignmentResponses.AddAsync(entity);
            await _unitOfWork.CloudinaryFiles.AddRangeAsync(entity.Files);
            await _unitOfWork.CommitAsync();

            return ToDto(entity);
        }

        public async Task<IEnumerable<AssignmentResponseDTO>> GetAllByTopicIdAsync(Guid topicId)
        {
            var entities = await _unitOfWork.AssignmentResponses.GetAllByTopicIdWithFilesAsync(topicId);
            return entities.Select(e => ToDto(e));
        }

        public async Task<AssignmentResponseDTO> UpdateByIdAsync(Guid id, UpdateAssignmentResponseRequest dto)
        {
            var entity = await _unitOfWork.AssignmentResponses.GetByIdTrackedWithFilesAsync(id);
            if (entity == null)
            {
                throw new Exception("Assignment response not found");
            }

            entity.TopicId = dto.TopicId;
            entity.StudentId = dto.StudentId;
            entity.SubmittedAt = dto.Data.SubmittedAt;
            entity.Note = dto.Data.Note;
            entity.Mark = dto.Data.Mark;

            await _unitOfWork.CloudinaryFiles.DeleteRangeAsync(entity.Files);
            entity.Files.Clear();

            var newFiles = dto.Data.Files;
            foreach (var file in newFiles)
            {
                file.Id = Guid.NewGuid();
                file.AssignmentResponseId = id;
                entity.Files.Add(file);
            }

            await _unitOfWork.CloudinaryFiles.AddRangeAsync(entity.Files);
            await _unitOfWork.CommitAsync();

            return ToDto(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _unitOfWork.AssignmentResponses.GetByIdTrackedWithFilesAsync(id);
            if (entity == null)
            {
                throw new Exception("Assignment response not found");
            }

            await _unitOfWork.AssignmentResponses.DeleteAsync(entity);
            await _unitOfWork.CommitAsync();
        }
    }
}
