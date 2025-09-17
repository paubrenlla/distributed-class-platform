using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class InscriptionRepository
    {
        private readonly List<Inscription> _inscriptions = new List<Inscription>();

        public Inscription Add(Inscription inscription)
        {
            _inscriptions.Add(inscription);
            return inscription;
        }

        public List<Inscription> GetActiveClassByClassId(int classId)
        {
            return _inscriptions.Where(i => i.Class.Id == classId && i.Status == InscriptionStatus.Active).ToList();
        }

        public Inscription GetActiveByUserAndClass(int userId, int classId)
        {
            return _inscriptions.FirstOrDefault(i => i.User.Id == userId && i.Class.Id == classId && i.Status == InscriptionStatus.Active);
        }
        
        public List<Inscription> GetByUser(int userId)
        {
            return _inscriptions.Where(i => i.User.Id == userId).ToList();
        }
    }
}