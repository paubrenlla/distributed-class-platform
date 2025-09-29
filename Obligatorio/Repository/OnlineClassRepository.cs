using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class OnlineClassRepository
    {
        private readonly List<OnlineClass> _clases = new List<OnlineClass>();
        private readonly object _lock = new object();

        public OnlineClass Add(OnlineClass clase)
        {
            if (clase == null) throw new ArgumentNullException(nameof(clase));
            lock (_lock)
            {
                _clases.Add(clase);
                return clase;
            }
        }

        public OnlineClass GetById(int id)
        {
            lock (_lock)
            {
                return _clases.FirstOrDefault(c => c.Id == id);
            }
        }

        public List<OnlineClass> GetAll()
        {
            lock (_lock)
            {
                return new List<OnlineClass>(_clases);
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
            lock (_lock)
            {
                var clase = _clases.FirstOrDefault(c => c.Id == classId);
                if (clase == null) throw new InvalidOperationException("Clase no encontrada");

                clase.Modificar(newName, newDesc, newCapacity, newDate, newDuration, inscripcionesActuales);
            }
        }


        public void Delete(int id)
        {
            lock (_lock)
            {
                var clase = GetById(id);
                if (clase == null)
                    throw new InvalidOperationException("Clase no encontrada");

                _clases.Remove(clase);
            }
        }

        public void EnsureImageNameIsUnique(int classId, string fileName)
        {
            lock (_lock)
            {
                var conflict = _clases.Any(c => c.Id != classId && c.Image == fileName);
                if (conflict)
                    throw new InvalidOperationException($"El nombre de imagen '{fileName}' ya está siendo usado");
            }
        }

        public void UpdateImage(int classId, string newFileName)
        {
            lock (_lock)
            {
                var clase = _clases.FirstOrDefault(c => c.Id == classId);
                if (clase == null) throw new InvalidOperationException("Clase no encontrada");

                clase.Image = newFileName;
            }
        }

    }
}
