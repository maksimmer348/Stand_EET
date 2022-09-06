using System.Collections.ObjectModel;

namespace StandETT;

public class TestVipReport
{
    private ObservableCollection<Vip> testedVipReport;
    public void TestedReport(Vip testedVip)
    {
        testedVipReport.Add(testedVip);
        //TODO по окончанию или по ходу испытаний отсюда данные буду добавлятся в TelerikReport
    }
}