using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StandETT;
/// <summary>
/// INotifyPropertyChanged уведомляет о том что внутри обьекта напмрие в Model измениилось какоето свойство.
/// при этом интерфейсная часть View подключится к этому интерфейсу будет слеждить за свойсвтом к кторму подключеня
/// и если это свойство изменилось View его перечитает. и обновит визуальную свою часть.
/// </summary>
public abstract class Notify : INotifyPropertyChanged
    {
        /// <summary>
        /// Событие возникает при изменении значениея свойсва (например в Model)
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Генерация события PropertyChanged, 
        /// </summary>
        /// <param name="propertyName">Имя свойсвта</param>
        /// исопльзуя [CallerMemberName] позволеят не указывать имя свойсвтва
        /// компилятор автоматически подставит имя ссвойства из котрогго было вызвано данная процедура.
        [NotifyPropertyChangedInvocator] //позволяет сделать свойство с уведомлениеем через конеткстное меню
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            //внутри генерируется сообытие которое возникает при изменении значениея свойсва (например в Model)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // При изменении одного свойства система атоматически может обновить второе свойство, а второе свойство обновит 3,
        // а 3 совйство обноит 1, и чтобыы эти колцевые обнонвления не зацикливались и не приводили к преполнению стека.
        /// <summary>
        /// Проверка если значение filed, которое хотиим обновить уже сответвует тому value которое передали то false, 
        /// а вот если значение filed изменилось то true, обновляем поле свойства и генерируем событиие OnPropertyChanged
        /// </summary>
        /// <param name="filed">Ссылка на поле свойства</param>
        /// <param name="value">Новое значение свойсва которое нужно установитиь</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <typeparam name="T">Тип свойства</typeparam>
        /// <returns></returns>
        protected virtual bool Set<T>(ref T filed, T value, [CallerMemberName] string propertyName = null, params string[] dependentPropertyNames)
        {
            if (Equals(filed, value))
            {
                return false;
            }

            filed = value;
            OnPropertyChanged(propertyName);

        
            foreach (var dependentPropertyName in dependentPropertyNames)
            {
                OnPropertyChanged(dependentPropertyName);
            }

            return true;
        }
    }

