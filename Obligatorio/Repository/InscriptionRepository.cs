using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Domain;

namespace Repository
{
    public class InscriptionRepository
    {
        private readonly List<Inscription> _inscriptions = new List<Inscription>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public Inscription Add(Inscription inscription)
        {
            if (inscription == null) throw new ArgumentNullException(nameof(inscription));

            _semaphore.Wait();
            try
            {
                _inscriptions.Add(inscription);
                return inscription;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<Inscription> GetActiveClassByClassId(int classId)
        {
            _semaphore.Wait();
            try
            {
                return _inscriptions
                    .Where(i => i.Class.Id == classId && i.Status == InscriptionStatus.Active)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Inscription GetActiveByUserAndClass(int userId, int classId)
        {
            _semaphore.Wait();
            try
            {
                return _inscriptions.FirstOrDefault(i =>
                    i.User.Id == userId &&
                    i.Class.Id == classId &&
                    i.Status == InscriptionStatus.Active);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<Inscription> GetByUser(int userId)
        {
            _semaphore.Wait();
            try
            {
                return _inscriptions
                    .Where(i => i.User.Id == userId)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<Inscription> GetAll()
        {
            _semaphore.Wait();
            try
            {
                return new List<Inscription>(_inscriptions);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}