import "./MovieViewer.css"
import { useState, forwardRef, useImperativeHandle } from "react";
import { Card, Pagination, Button, message, Menu, Dropdown, Modal, Spin, Descriptions, Image, Input, Space } from 'antd';
import { PlusCircleOutlined, ArrowDownOutlined, UserOutlined, GroupOutlined, TagOutlined, HeartFilled, HeartOutlined, CaretRightOutlined } from '@ant-design/icons';
import { createPotPlayerPlayList, addToDefaultPotPlayerPlayList, getMoivesByFilter, getMovieDetails, getMoviesWildcardSearch, likeMovie } from "../services/DataService";

const { Meta } = Card;
const { Search } = Input;

const MovieViewer = forwardRef((props, ref) => {
    const [numEachPage, setNumEachPage] = useState(16);
    const [minValue, setMinValue] = useState(0);
    const [maxValue, setMaxValue] = useState(numEachPage);
    const [movies, setMovies] = useState([]);
    const [movie, setMovie] = useState(null);
    const [visible, setVisible] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isLikeLoading, setIsLikeLoading] = useState(false);
    const [likeFlag, setLikeFlag] = useState(0);
    const [pageRoot, setPageRoot] = useState("");

    useImperativeHandle(ref, () => ({
        initializeMovies(movies, numEachPage = 14, pageRoot = "") {
            init(movies, numEachPage, pageRoot);
        },
        setIsLoading() {
            setIsLoading(true);
        }
    }));

    function init(movies, numEachPage, pageRoot) {
        setNumEachPage(numEachPage);
        setMinValue(0);
        setMaxValue(numEachPage);
        setMovies(movies);
        setIsLoading(false);
        if(pageRoot === "") {
            let today = new Date();
            pageRoot = `${today.getFullYear()}-${today.getMonth()}-${today.getDate()}`;
        }
        setPageRoot(pageRoot);
    }

    function handleChange(value) {
        setMinValue((value - 1) * numEachPage);
        setMaxValue(value * numEachPage);
    };

    function createPotPlayList() {
        let movieLocations = [];
        for (let i = 0; i < movies.length; ++i) {
            movieLocations.push({ imdbId: movies[i].imdbId, movieLocation: movies[i].movieLocation });
        }
        createPotPlayerPlayList(movieLocations, pageRoot).then(() => {
            message.info("加入完毕");
        }).catch((error) => {
            console.log(error);
            message.info("加入失败!");
        });
    }

    function sortByReleasedDate() {
        setIsLoading(true);
        setTimeout(() => {
            movies.sort(function (a, b) {
                var x = a.dateAdded;
                var y = b.dateAdded;
                return x > y ? -1 : x < y ? 1 : 0;
            });
            setMovies(movies);
            setIsLoading(false);
        }, 100);
    }

    function sortByTitle() {
        setIsLoading(true);
        setTimeout(() => {
            movies.sort(function (a, b) {
                var x = a.imdbId.toLowerCase();
                var y = b.imdbId.toLowerCase();
                return x < y ? -1 : x > y ? 1 : 0;
            });
            setMovies(movies);
            setIsLoading(false);
        }, 100);
    }

    function showMovieDetails(movieIndex) {
        getMovieDetails(movies[movieIndex]).then(resp => {
            setMovie(resp);
            setLikeFlag(movies[movieIndex].liked);
            setVisible(true);
        })
    }

    function loadMovies(filterType, filter) {
        setVisible(false);
        setIsLoading(true);
        getMoivesByFilter(filterType, filter).then(resp => {
            init(resp, 16);
        });
    }

    function playMovie() {
        setVisible(false);
        addToDefaultPotPlayerPlayList([{ imdbId: movie.imdbId, movieLocation: movie.movieLocation }]).then(() => {
            message.info("加入完毕");
        }).catch((error) => {
            console.log(error);
            message.info("加入失败!");
        });
    }

    function onSearch(value) {
        setIsLoading(true);
        getMoviesWildcardSearch(value).then(resp => {
            init(resp, 16, value);
        }).catch(error => console.log(error));
    }

    function onLikeClick() {
        setIsLikeLoading(true);
        likeMovie(movie?.imdbId).then(resp => {
            setIsLikeLoading(false);
            setLikeFlag(resp);
        }).catch(error => console.log(error));
    }

    const menu = (
        <Menu>
            <Menu.Item>
                <a onClick={sortByReleasedDate}>
                    按日期
                </a>
            </Menu.Item>
            <Menu.Item>
                <a onClick={sortByTitle}>
                    按名称
                </a>
            </Menu.Item>
        </Menu>
    );

    const movieDetailsTitle = ([<Space>
        <Button key="movie-like-btn"
            shape="circle"
            icon={likeFlag === true ? <HeartFilled /> : <HeartOutlined />}
            onClick={onLikeClick}
            loading={isLikeLoading}></Button>
        <Button key="movie-play-btn"
            shape="circle"
            icon={<CaretRightOutlined />}
            onClick={playMovie}></Button>
    </Space>])

    return (
        <div className="movie-viewer">
            {isLoading ? "" :
                <Pagination
                    simple
                    defaultCurrent={1}
                    defaultPageSize={numEachPage} //default size of page
                    onChange={handleChange}
                    total={movies?.length}
                    className="header-left"
                    disabled={isLoading}
                />}
            <div className="header-right">
                <Search placeholder="影片名" onSearch={onSearch} className="header-element-right movie-search-bar" loading={isLoading} />
                <Button
                    type="primary"
                    icon={<PlusCircleOutlined />}
                    disabled={movies?.length === 0 || isLoading ? true : false}
                    onClick={createPotPlayList}
                    className="header-element-right">
                    加入PotPlayer列表
                </Button>
                <Dropdown overlay={menu} arrow className="header-element-right" disabled={isLoading}>
                    <Button icon={<ArrowDownOutlined />}>排序</Button>
                </Dropdown>
            </div>
            {isLoading ? <div><Spin size="large" /></div> : (
                <div className="movie-list">
                    {movies?.slice(minValue, maxValue).map((movie, i) =>
                        <Card
                            className="poster-card"
                            key={"movie-" + i + minValue}
                            hoverable
                            // cover={<img className="poster-image" />}
                            cover={<img className="image" src={movie.posterFileLocation} />}
                            onClick={() => showMovieDetails(i + minValue)}
                        >
                            <Meta title={movie.imdbId} description={movie.title} />
                        </Card>)}
                </div>

            )}
            <Modal
                title={movieDetailsTitle}
                centered
                visible={visible}
                onOk={() => setVisible(false)}
                onCancel={() => setVisible(false)}
                width={1000}
                className="movie-details"
            >
                <Card
                    hoverable
                    cover={<Image className="fanart-image" src={movie?.fanArtLocation} />}
                    className="fanart-card"
                ></Card>
                <Descriptions title={movie?.title} bordered>
                    <Descriptions.Item label="标题">{movie?.title}</Descriptions.Item>
                    <Descriptions.Item label="工作室">{movie?.studio}</Descriptions.Item>
                    <Descriptions.Item label="加入时间">{movie?.dateAdded}</Descriptions.Item>
                    <Descriptions.Item label="发布时间">{movie?.releaseDate}</Descriptions.Item>
                    <Descriptions.Item label="播放时间">{movie?.runtime}分钟</Descriptions.Item>
                    <Descriptions.Item label="播放次数">{movie?.playedCount}</Descriptions.Item>
                    <Descriptions.Item label="情节" span={3}>{movie?.plot}</Descriptions.Item>
                    <Descriptions.Item label="演员" span={3}>
                        {movie?.actors.map((actor, i) => <Button className="modal-button" icon={<UserOutlined />} key={"actor-desc-" + i} onClick={() => loadMovies(0, [actor.name])}>{actor.name}</Button>)}
                    </Descriptions.Item>
                    <Descriptions.Item label="类型" span={3}>
                        {movie?.genres.map((genre, i) => <Button className="modal-button" icon={<GroupOutlined />} key={"actor-desc-" + i} onClick={() => loadMovies(1, [genre.name])}>{genre.name}</Button>)}
                    </Descriptions.Item>
                    <Descriptions.Item label="标签" span={3}>
                        {movie?.tags.map((tag, i) => <Button className="modal-button" icon={<TagOutlined />} key={"tag-desc-" + i} onClick={() => loadMovies(2, [tag.name])}>{tag.name}</Button>)}
                    </Descriptions.Item>
                </Descriptions>
            </Modal>
        </div>
    )

});
export default MovieViewer;