using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class OnlineClassRepository
    {
        private readonly List<ClaseOnline> _clases = new List<ClaseOnline>();

        public ClaseOnline Add(ClaseOnline clase)
        {
            if (clase == null) throw new ArgumentNullException(nameof(clase));
            _clases.Add(clase);
            return clase;
        }

        public ClaseOnline GetById(int id)
        {
            return _clases.FirstOrDefault(c => c.Id == id);
        }

        public List<ClaseOnline> GetAll()
        {
            return new List<ClaseOnline>(_clases);
        }

        public List<ClaseOnline> SearchByKeyword(string keyword)
        {
            return _clases
                .Where(c => c.Nombre.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            c.Descripcion.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void Update(ClaseOnline clase)
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
