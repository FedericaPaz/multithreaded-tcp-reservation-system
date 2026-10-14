using Common.Config;
using Common.Models;
using Server.Repositories;

namespace Server;

/*
 *  una reserva no se confirma ni se rechaza ne le momento, queda en pendiente y se resuelve n segundos dps
 * eso se configura en app configuration
 *
 * se levanta un thread nuevo por cada reserva, porque la letra pide usar thread como mecanismo de concurrencia, por ende
 * cada reserva queda como una unidad de trabajo independiente, las reservas no commparten estado propio entre si, cada una
 * de las reservas solo toca el reservation y vehicle repository que ya tienen lock interno.
 *
 * los threads se marcan como IsBackground para no impedir el cierre del proceso.
 */

public class ReservationScheduler
{
    public static void Schedule(Reservation reservation)
    {
        int delayedSeconds = new SettingsManager().ReadIntSetting("ReservationProcessingSeconds");
        Thread worker = new Thread(() => Resolve(reservation, delayedSeconds)) //hilo para cada reserva
        {
            IsBackground = true,
            Name = $"ReservationHandler{reservation.Id}"
        };
        worker.Start();
    }
    
    private static void Resolve(Reservation reservation, int delayedSeconds)
    {
        Thread.Sleep(TimeSpan.FromSeconds(delayedSeconds));
        
        // busco el stock del auto pq el modelo pudo haber sido modificado mientras se esperaba
        Vehicle? vehicle = VehicleRepository.Instance.GetVehicle(reservation.ModelName);
        int currentStock = vehicle?.InitialStock ?? 0;

        ReservationRepository.Instance.Resolve(reservation.Id, currentStock);

        Reservation? resolved = ReservationRepository.Instance.Get(reservation.Id);
        if (resolved == null || resolved.Status != ReservationStatus.Confirmed)
            return;

        HistoryRepository.Instance.Register(new HistoryEvent
        {
            Username = resolved.Username,
            Type = EventType.Purchase,
            Detail = $"Reservation {resolved.Id} for '{resolved.ModelName}' was confirmed."
        });
    }
}