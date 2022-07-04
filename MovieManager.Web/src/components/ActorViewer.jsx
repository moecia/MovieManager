import "./ActorViewer.css"
import { useState, forwardRef, useImperativeHandle, useRef } from "react";
import { Pagination, Button, Spin, Modal, Descriptions, Input } from 'antd';
import { HeartFilled, HeartOutlined } from '@ant-design/icons';
import { getActorByName, likeActor, getMoivesByFilter } from "../services/DataService";
import MovieViewer from "./MovieViewer";

const { Search } = Input;

const ActorViewer = forwardRef((props, ref) => {
    const numEachPage = 60;

    const [minValue, setMinValue] = useState(0);
    const [maxValue, setMaxValue] = useState(numEachPage);
    const [actors, setActors] = useState([]);
    const [actor, setActor] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [visible, setVisible] = useState(false);
    const [isLikeLoading, setIsLikeLoading] = useState(false);
    const [likeFlag, setLikeFlag] = useState(0);

    const movieViewer = useRef();

    useImperativeHandle(ref, () => ({
        initializeActors(actors) {
            init(actors);
        },
        setIsLoading() {
            setIsLoading(true);
        }
    }));

    function init(actors) {
        setMinValue(0);
        setMaxValue(numEachPage);
        setActors(actors);
        setIsLoading(false);
    }

    function handleChange(value) {
        setMinValue((value - 1) * numEachPage);
        setMaxValue(value * numEachPage);
    };

    function showActorDetails(actorIndex) {
        getActorByName(actors[actorIndex]).then(resp => {
            setActor(resp[0]);
            setVisible(true);
            movieViewer?.current.setIsLoading();
            setLikeFlag(resp[0].liked);
            getMoivesByFilter(0, [resp[0].name], false).then(resp => {
                movieViewer?.current.initializeMovies(resp, 5, actors[actorIndex]);
            });
        }).catch((error) => {
            console.log(error);
        });
    }

    function onSearch(value) {
        setIsLoading(true);
        getActorByName(value).then(resp => {
            let actors = resp ? resp.map(x => x.name) : [];
            init(actors);
        }).catch(error => console.log(error));
    }

    function onLikeClick() {
        setIsLikeLoading(true);
        likeActor(actor?.name).then(resp => {
            setIsLikeLoading(false);
            setLikeFlag(resp);
        }).catch(error => console.log(error));
    }

    return (
        <div className="actor-viewer">
            {isLoading ? <div><Spin size="large" /></div> :
                <Pagination
                    simple
                    defaultCurrent={1}
                    defaultPageSize={numEachPage} //default size of page
                    onChange={handleChange}
                    total={actors?.length}
                    className="header-left"
                />}
            <Search placeholder="演员名" onSearch={onSearch} className="header-right actor-search-bar" loading={isLoading} />
            {isLoading ? <div><Spin size="large" /></div> :
                <div>
                    <div className="actor-list">
                        {actors?.slice(minValue, maxValue).map((actor, i) =>
                            <Button key={"actor-" + i + minValue} className="actor-button" onClick={() => showActorDetails(i + minValue)}>
                                {actor}
                            </Button>)}
                    </div>
                </div>}
            <Modal
                title={[<Button key="actor-like-btn"
                    shape="circle"
                    icon={likeFlag === true ? <HeartFilled /> : <HeartOutlined />}
                    onClick={onLikeClick}
                    loading={isLikeLoading}></Button>]}
                centered
                visible={visible}
                onOk={() => setVisible(false)}
                onCancel={() => setVisible(false)}
                width={1100}
                className="actor-details"
            >
                <Descriptions title={actor?.name} bordered>
                    <Descriptions.Item label="姓名" span={2}>{actor?.name}</Descriptions.Item>
                    <Descriptions.Item label="生日">{actor?.dateofBirth}</Descriptions.Item>
                    <Descriptions.Item label="身高" span={2}>{actor?.height}</Descriptions.Item>
                    <Descriptions.Item label="罩杯">{actor?.cup}</Descriptions.Item>
                </Descriptions>
                <MovieViewer ref={movieViewer} />
            </Modal>
        </div>
    )
});
export default ActorViewer;