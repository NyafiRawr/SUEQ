const Response = require('../utils/response');
const dayNames = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];

module.exports = (sequelize, Sequelize) => {
    const Model = sequelize.define(
        'schedule',
        {
            id: {
                type: Sequelize.INTEGER,
                primaryKey: true,
                autoIncrement: true,
                allowNull: false,
            },
            queueId: {
                type: Sequelize.INTEGER,
                allowNull: false,
            },
            // Время начала рабочего дня
            startTime: {
                type: Sequelize.TIME,
                allowNull: false,
            },
            // Время окончания рабочего дня
            endTime: {
                type: Sequelize.TIME,
                allowNull: false,
            },
            // Дни недели: битовая маска бит1(2)=Пн, бит7(128)=Вс, Пн-Пт=62
            weekday: {
                type: Sequelize.INTEGER,
                defaultValue: 62,
                allowNull: false,
                get() {
                    const rest = [];
                    let result = this.getDataValue('weekday');
                    while (rest.length !== 7) {
                        result = Math.floor(result / 2);
                        rest.push(result % 2);
                    }
                    return rest
                        .map((day, index) => {
                            if (day === 1) {
                                return dayNames[index];
                            }
                            return null;
                        })
                        .filter((value) => value !== null);
                },
            },
            //  Дата начала действия записи расписания
            workFrom: {
                type: Sequelize.DATEONLY,
                defaultValue: Sequelize.NOW,
                allowNull: false,
            },
            // Дата окончания действия
            workTo: {
                type: Sequelize.DATEONLY,
                defaultValue: '2077-01-01', // Предположим наше приложение проработает 56 лет. Удобнее, чем работать с null
                allowNull: false,
            },
        },
        {
            paranoid: false,
            timestamps: true,
        }
    );

    //#region Методы класса

    Model.findByScheduleId = async (id) => {
        const result = await Model.findByPk(id);

        if (result === null) {
            throw new Response('Такого расписания не существует.');
        }

        return result;
    };

    Model.findAllByQueueId = async (id) => {
        return await Model.findAll({
            where: { queueId: id },
        });
    };

    //#endregion

    return Model;
};
