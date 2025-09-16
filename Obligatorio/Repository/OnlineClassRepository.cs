using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class OnlineClassRepository
    {
        private readonly List<OnlineClass> _clases = new List<OnlineClass>();

        public OnlineClass Add(OnlineClass clase)
        {
            if (clase == null) throw new ArgumentNullException(nameof(clase));
            _clases.Add(clase);
            return clase;
        }

        public OnlineClass GetById(int id)
        {
            return _clases.FirstOrDefault(c => c.Id == id);
        }

        public List<OnlineClass> GetAll()
        {
            return new List<OnlineClass>(_clases);
        }

        public List<OnlineClass> SearchByKeyword(string keyword)
        {
            return _clases
                .Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            c.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void Update(OnlineClass clase)
        {
            var existing = GetById(clase.Id);
            if (existing == null) throw new InvalidOperationException("Clase no encontrada");
        }

        public void Delete(int id)
        {
            var clase = GetById(id);
            if (clase == null) throw new InvalidOperationException("Clase no encontrada");

            _clases.Remove(clase);
        }
    }
}
