using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Domain;

namespace Repository
{
    public class OnlineClassRepository
    {
        private readonly List<OnlineClass> _clases = new List<OnlineClass>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public OnlineClass Add(OnlineClass clase)
        {
            if (clase == null) throw new ArgumentNullException(nameof(clase));

            _semaphore.Wait();
            try
            {
                _clases.Add(clase);
                return clase;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public OnlineClass GetById(int id)
        {
            _semaphore.Wait();
            try
            {
                return _clases.FirstOrDefault(c => c.Id == id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<OnlineClass> GetAll()
        {
            _semaphore.Wait();
            try
            {
                return new List<OnlineClass>(_clases);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<OnlineClass> SearchByKeyword(string keyword)
        {
            return _clases
                .Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            c.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void ModifyClass(int classId, string newName, string newDesc, string newCapacity,
            string newDuration, string newDate, int inscripcionesActuales)
        {
            _semaphore.Wait();
            try
            {
                var clase = _clases.FirstOrDefault(c => c.Id == classId);
                if (clase == null) throw new InvalidOperationException("Clase no encontrada");

                clase.Modificar(newName, newDesc, newCapacity, newDate, newDuration, inscripcionesActuales);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Delete(int id)
        {
            _semaphore.Wait();
            try
            {
                var clase = GetById(id);
                if (clase == null)
                    throw new InvalidOperationException("Clase no encontrada");

                _clases.Remove(clase);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void EnsureImageNameIsUnique(int classId, string fileName)
        {
            _semaphore.Wait();
            try
            {
                var conflict = _clases.Any(c => c.Id != classId && c.Image == fileName);
                if (conflict)
                    throw new InvalidOperationException($"El nombre de imagen '{fileName}' ya está siendo usado");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void UpdateImage(int classId, string newFileName)
        {
            _semaphore.Wait();
            try
            {
                var clase = _clases.FirstOrDefault(c => c.Id == classId);
                if (clase == null) throw new InvalidOperationException("Clase no encontrada");

                clase.Image = newFileName;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public OnlineClass GetByLink(string dtoLink)
        {
            _semaphore.Wait();
            try
            {
                var clase = _clases.FirstOrDefault(c => c.Link == dtoLink);
                if (clase == null) throw new InvalidOperationException("Clase no encontrada");
                return clase;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
